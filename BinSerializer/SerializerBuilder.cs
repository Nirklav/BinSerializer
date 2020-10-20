using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using ThirtyNineEighty.BinarySerializer.Types;

namespace ThirtyNineEighty.BinarySerializer
{
  delegate void Writer<in T>(Stream stream, T obj);
  delegate T Reader<out T>(Stream stream);

  static class SerializerBuilder
  {
    private static readonly ConcurrentDictionary<TypeImpl, Delegate> Writers = new ConcurrentDictionary<TypeImpl, Delegate>();
    private static readonly ConcurrentDictionary<TypeImpl, Delegate> Readers = new ConcurrentDictionary<TypeImpl, Delegate>();

    #region writer
    [SecurityCritical]
    public static Writer<T> CreateWriter<T>(TypeImpl type)
    {
      var method = Writers.GetOrAdd(type, CreateWriterMethod);
      return MethodAdapter.CastWriter<T>(method, type);
    }

    [SecuritySafeCritical]
    private static Delegate CreateWriterMethod(TypeImpl type)
    {
      var methodName = $"{ type }_writer";
      var dynamicMethod = new DynamicMethod(methodName, typeof(void), new[] { typeof(Stream), type.Type }, typeof(SerializerBuilder), true);

      var il = dynamicMethod.GetILGenerator();

      if (!type.TypeInfo.IsArray)
        GenerateObjectWriter(il, type);
      else      
        GenerateArrayWriter(il, type);

      return dynamicMethod.CreateDelegate(typeof(Writer<>).MakeGenericType(type.Type));
    }

    [SecurityCritical]
    private static void GenerateObjectWriter(ILGenerator il, TypeImpl type)
    {
      var getRefId = typeof(RefWriterWatcher)
        .GetTypeInfo()
        .GetMethod(nameof(RefWriterWatcher.GetRefId), BindingFlags.Public | BindingFlags.Static);
      var onSerializing = typeof(IBinSerializable).GetMethod(nameof(IBinSerializable.OnSerializing));

      il.DeclareLocal(typeof(bool));
      il.DeclareLocal(typeof(SerializationInfo));

      var skipTypeLabel = il.DefineLabel();

      BSDebug.TraceStart(il, "Write " + type.TypeInfo.Name);

      // Invoke deserialzation callback
      if (typeof(IBinSerializable).IsAssignableFrom(type.Type))
      {
        il.Emit(OpCodes.Ldloca_S, (byte)1);       // Load result local address
        il.Emit(OpCodes.Initobj, typeof(SerializationInfo));

        il.Emit(OpCodes.Ldarg_1);                 // Load serializing object
        il.Emit(OpCodes.Ldloc_1);                 // Load created SerializationInfo
        il.Emit(OpCodes.Callvirt, onSerializing); // Call onSerializing
      }

      // Write type
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldstr, SerializerTypes.GetTypeId(type));
      il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamWriter(typeof(string)));

      if (!type.TypeInfo.IsValueType)
      {
        // Write refId
        il.Emit(OpCodes.Ldarg_0);           // Load stream
        il.Emit(OpCodes.Ldarg_1);           // Load obj
        il.Emit(OpCodes.Ldloca_S, (byte)0); // Load address of bool local
        il.Emit(OpCodes.Call, getRefId);    // Obj => RefId
        il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamWriter(typeof(int)));

        // Check case when reference type already serialized.
        // If null be returned then created flag be zero too and serialization will be skipped.
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Brfalse, skipTypeLabel);
      }

      // Write type version
      var version = SerializerTypes.GetVersion(type);

      il.Emit(OpCodes.Ldarg_0);         // Load stream
      il.Emit(OpCodes.Ldc_I4, version); // Load version
      il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamWriter(typeof(int)));

      // If writer exist then just write
      var writer = SerializerTypes.TryGetStreamWriter(type) ?? SerializerTypes.TryGetTypeWriter(type);
      if (writer != null)
      {
        il.Emit(OpCodes.Ldarg_0);      // Load stream
        il.Emit(OpCodes.Ldarg_1);      // Load obj
        il.Emit(OpCodes.Call, writer); // Write to stream
      }
      else
      {
        foreach (var binField in BinField.Get(type.Type))
        {
          var skipFieldLabel = il.DefineLabel();

          // If field is null then skip it
          if (!binField.IsValueType)
          {
            il.Emit(OpCodes.Ldarg_1);                 // Load object
            binField.EmitRead(il);                    // Read field form object
            il.Emit(OpCodes.Brfalse, skipFieldLabel); // Skip if field is null
          }

          // Write field id
          il.Emit(OpCodes.Ldarg_0);             // Load stream
          il.Emit(OpCodes.Ldstr, binField.Id);  // Load field id
          il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamWriter(typeof(string)));

          var serialize = typeof(BinSerializer<>)
            .MakeGenericType(binField.Type)
            .GetTypeInfo()
            .GetMethod(nameof(BinSerializer<object>.Serialize), BindingFlags.Static | BindingFlags.Public);

          // Write field
          il.Emit(OpCodes.Ldarg_0);         // Load stream
          il.Emit(OpCodes.Ldarg_1);         // Load object      
          binField.EmitRead(il);            // Read field from object
          il.Emit(OpCodes.Call, serialize); // Write field value

          // Skip field label
          il.MarkLabel(skipFieldLabel);
        }

        // Write end id
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldstr, SerializerTypes.TypeEndToken);
        il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamWriter(typeof(string))); 
      }

      // Skip type label
      il.MarkLabel(skipTypeLabel);

      BSDebug.TraceEnd(il, "Write " + type.TypeInfo.Name);

      // End
      il.Emit(OpCodes.Ret);
    }

    [SecurityCritical]
    private static void GenerateArrayWriter(ILGenerator il, TypeImpl type)
    {
      var elementType = type.TypeInfo.GetElementType();

      var getRefId = typeof(RefWriterWatcher)
        .GetTypeInfo()
        .GetMethod(nameof(RefWriterWatcher.GetRefId), BindingFlags.Public | BindingFlags.Static);

      var getLowerBound = typeof(Array)
        .GetTypeInfo()
        .GetMethod(nameof(Array.GetLowerBound), BindingFlags.Instance | BindingFlags.Public);

      var serialize = typeof(BinSerializer<>)
        .MakeGenericType(elementType)
        .GetTypeInfo()
        .GetMethod(nameof(BinSerializer<object>.Serialize), BindingFlags.Static | BindingFlags.Public);

      il.DeclareLocal(typeof(int));  // Array length
      il.DeclareLocal(typeof(int));  // Array index
      il.DeclareLocal(typeof(bool)); // Ref id created flag

      var skipLabel = il.DefineLabel();
      var zeroBased = il.DefineLabel();
      var loopLabel = il.DefineLabel();
      var exitLoopLabel = il.DefineLabel();

      BSDebug.TraceStart(il, "Write " + type.TypeInfo.Name);

      // Write type
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldstr, SerializerTypes.GetTypeId(type));
      il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamWriter(typeof(string)));

      // Write refId
      il.Emit(OpCodes.Ldarg_0);           // Load stream
      il.Emit(OpCodes.Ldarg_1);           // Load obj
      il.Emit(OpCodes.Ldloca_S, (byte)2); // Load address of bool local
      il.Emit(OpCodes.Call, getRefId);    // Obj => RefId
      il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamWriter(typeof(int)));

      // Check case when reference type already serialized.
      // If null be returned then created flag be zero too and serialization will be skipped.
      il.Emit(OpCodes.Ldloc_2);
      il.Emit(OpCodes.Brfalse, skipLabel);

      var dim = type.TypeInfo.GetArrayRank();
      if (dim == 1)
      {
        // Dimension check
        il.Emit(OpCodes.Ldarg_1);                 // Load array
        il.Emit(OpCodes.Ldc_I4_0);                // Load zero (0 dim)
        il.Emit(OpCodes.Callvirt, getLowerBound); // Get lower bound of 0 dim
        il.Emit(OpCodes.Brfalse, zeroBased);      // If result 0 then jump to zeroBased

        // Throw exception if non zero based
        il.Emit(OpCodes.Ldstr, "Non zero based arrays not supported.");
        il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetTypeInfo().GetConstructor(new[] { typeof(string) }));
        il.Emit(OpCodes.Throw);

        // Write array length
        il.MarkLabel(zeroBased);
        il.Emit(OpCodes.Ldarg_0);         // Load stream
        il.Emit(OpCodes.Ldarg_1);         // Load array
        il.Emit(OpCodes.Ldlen);           // Load array length
        il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamWriter(typeof(int)));

        // Set array length
        il.Emit(OpCodes.Ldarg_1); // Load array
        il.Emit(OpCodes.Ldlen);   // Load array length
        il.Emit(OpCodes.Stloc_0); // Set to 0 local

        // Set array index to 0
        il.Emit(OpCodes.Ldc_I4_0); // Load 0
        il.Emit(OpCodes.Stloc_1);  // Set to 1 local

        // Loop start
        il.MarkLabel(loopLabel);

        // Loop check
        il.Emit(OpCodes.Ldloc_0);            // Load array length
        il.Emit(OpCodes.Ldloc_1);            // Load current index
        il.Emit(OpCodes.Beq, exitLoopLabel); // If equals then exit

        // Write element to stream
        il.Emit(OpCodes.Ldarg_0);             // Load stream
        il.Emit(OpCodes.Ldarg_1);             // Load array
        il.Emit(OpCodes.Ldloc_1);             // Load current index
        il.Emit(OpCodes.Ldelem, elementType); // Load element
        il.Emit(OpCodes.Call, serialize);

        // Inrement
        il.Emit(OpCodes.Ldloc_1);  // Load current index
        il.Emit(OpCodes.Ldc_I4_1); // Load 1
        il.Emit(OpCodes.Add);      // Add
        il.Emit(OpCodes.Stloc_1);  // Set current index

        // Jump to loop start
        il.Emit(OpCodes.Br, loopLabel); 
      }
      else
      {
        throw new NotSupportedException("Arrays with non single dimension not supported.");
      }

      // Skipped
      il.MarkLabel(skipLabel);
      il.MarkLabel(exitLoopLabel);

      BSDebug.TraceEnd(il, "Write " + type.TypeInfo.FullName);

      // End
      il.Emit(OpCodes.Ret);
    }
    #endregion

    #region reader
    [SecurityCritical]
    public static Reader<T> CreateReader<T>(TypeImpl type)
    {
      var method = Readers.GetOrAdd(type, CreateReaderMethod);
      return MethodAdapter.CastReader<T>(method, type);
    }

    [SecurityCritical]
    private static Delegate CreateReaderMethod(TypeImpl type)
    {
      var methodName = $"{ type }_reader";
      var dynamicMethod = new DynamicMethod(methodName, type.Type, new[] { typeof(Stream) }, typeof(SerializerBuilder), true);

      var il = dynamicMethod.GetILGenerator();

      if (!type.TypeInfo.IsArray)
        GenerateObjectReader(il, type);
      else
        GenerateArrayReader(il, type);

      return dynamicMethod.CreateDelegate(typeof(Reader<>).MakeGenericType(type.Type));
    }

    [SecurityCritical]
    private static void GenerateObjectReader(ILGenerator il, TypeImpl type)
    {
      var addRef = typeof(RefReaderWatcher)
        .GetTypeInfo()
        .GetMethod(nameof(RefReaderWatcher.AddRef), BindingFlags.Public | BindingFlags.Static)
        .MakeGenericMethod(type.Type);

      var tryGetRef = typeof(RefReaderWatcher)
        .GetTypeInfo()
        .GetMethod(nameof(RefReaderWatcher.TryGetRef), BindingFlags.Public | BindingFlags.Static)
        .MakeGenericMethod(type.Type);

      var getTypeFromHandle = typeof(Type)
        .GetTypeInfo()
        .GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Public | BindingFlags.Static);

      var defaultCtor = type.TypeInfo.GetConstructor(Type.EmptyTypes);
      
      var getUninitializedObject = typeof(RuntimeHelpers)
        .GetTypeInfo()
        .GetMethod(nameof(FormatterServices.GetUninitializedObject), BindingFlags.Public | BindingFlags.Static);

      var dynamicObjectRead = typeof(DynamicObjectReader<>)
        .MakeGenericType(type.Type)
        .GetTypeInfo()
        .GetMethod(nameof(DynamicObjectReader<object>.Read), BindingFlags.Public | BindingFlags.Static);

      var stringEquals = typeof(string)
        .GetTypeInfo()
        .GetMethod(nameof(string.Equals), new[] { typeof(string), typeof(string), typeof(StringComparison) });

      var onDeserialized = typeof(IBinSerializable).GetMethod(nameof(IBinSerializable.OnDeserialized));


      il.DeclareLocal(typeof(int));                 // ref if
      il.DeclareLocal(type.Type);                        // readed object
      il.DeclareLocal(typeof(string));              // last readed field id
      il.DeclareLocal(typeof(int));                 // readed object version
      il.DeclareLocal(typeof(DeserializationInfo)); // info

      var resultLabel = il.DefineLabel();
      var readFastLabel = il.DefineLabel();

      // Process reference id
      if (!type.TypeInfo.IsValueType)
      {
        var tryLoadReferenceLabel = il.DefineLabel();

        // Read reference id
        il.Emit(OpCodes.Ldarg_0);                                               // Load stream
        il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamReader(typeof(int))); // Read ref id
        il.Emit(OpCodes.Dup);                                                   // Duplicate ref id
        il.Emit(OpCodes.Stloc_0);                                               // Set ref id to local

        // Check if result null
        il.Emit(OpCodes.Brtrue, tryLoadReferenceLabel); // Check if null was written

        // Null was written (return null)
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Stloc_1);         // Set result to null
        il.Emit(OpCodes.Br, resultLabel); // Jump to result

        // Try get readed reference
        il.MarkLabel(tryLoadReferenceLabel);
        il.Emit(OpCodes.Ldloc_0);             // Load reference id
        il.Emit(OpCodes.Ldloca_S, (byte)1);   // Load address of result
        il.Emit(OpCodes.Call, tryGetRef);
        il.Emit(OpCodes.Brtrue, resultLabel); // Jump to result if reference already exist
      }

      // Read version
      il.Emit(OpCodes.Ldarg_0);                                               // Load stream
      il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamReader(typeof(int))); // Read saved version
      il.Emit(OpCodes.Stloc_3);                                               // Save version to local

      var reader = SerializerTypes.TryGetStreamReader(type);
      if (reader != null)
      {
        // Read
        il.Emit(OpCodes.Ldarg_0); // Load stream
        il.Emit(OpCodes.Call, reader);
        il.Emit(OpCodes.Stloc_1); // Set result to local

        // Add reference to watcher
        if (!type.TypeInfo.IsValueType)
        {
          il.Emit(OpCodes.Ldloc_0); // Load reference id
          il.Emit(OpCodes.Ldloc_1); // Load result object reference
          il.Emit(OpCodes.Call, addRef);
        }
      }
      else
      {
        // Create
        if (!type.TypeInfo.IsValueType)
        {
          if (defaultCtor != null)
          {
            il.Emit(OpCodes.Newobj, defaultCtor);
            il.Emit(OpCodes.Stloc_1); // Set result to local
          }
          else
          {
            il.Emit(OpCodes.Ldtoken, type.Type);
            il.Emit(OpCodes.Call, getTypeFromHandle);
            il.Emit(OpCodes.Call, getUninitializedObject);
            il.Emit(OpCodes.Castclass, type.Type);
            il.Emit(OpCodes.Stloc_1); // Set result to local
          }
        }
        else
        {
          il.Emit(OpCodes.Ldloca_S, (byte)1); // Load result local address
          il.Emit(OpCodes.Initobj, type.Type);
        }

        // Add reference to watcher
        if (!type.TypeInfo.IsValueType)
        {
          il.Emit(OpCodes.Ldloc_0); // Load reference id
          il.Emit(OpCodes.Ldloc_1); // Load result object reference
          il.Emit(OpCodes.Call, addRef);
        }

        // Read code type version
        var version = SerializerTypes.GetVersion(type);

        il.Emit(OpCodes.Ldloc_3);             // Load version
        il.Emit(OpCodes.Ldc_I4, version);     // Load program type version 
        il.Emit(OpCodes.Beq, readFastLabel);

        // Call dynamic read if versions not equal
        il.Emit(OpCodes.Ldarg_0);                 // Load stream
        il.Emit(OpCodes.Ldloc_1);                 // Load obj instance
        il.Emit(OpCodes.Ldloc_3);                 // Load version
        il.Emit(OpCodes.Call, dynamicObjectRead); // Do dynamic read
        il.Emit(OpCodes.Stloc_1);                 // Set readed value to result local
        il.Emit(OpCodes.Br, resultLabel);         // Jump to return code part

        il.MarkLabel(readFastLabel);

        // Read
        var typeReader = SerializerTypes.TryGetTypeReader(type);
        if (typeReader != null)
        {
          // Read type
          il.Emit(OpCodes.Ldarg_0); // Load stream
          il.Emit(OpCodes.Ldloc_1); // Load obj instance
          il.Emit(OpCodes.Ldloc_3); // Load version
          il.Emit(OpCodes.Call, typeReader);
          il.Emit(OpCodes.Stloc_1); // Set result to local
        }
        else
        {
          // Read fields
          Label? nextFieldLabel = null;

          foreach (var binField in BinField.Get(type.Type))
          {
            var markCurrent = nextFieldLabel != null;
            if (nextFieldLabel == null)
              nextFieldLabel = il.DefineLabel();

            // Read field id
            il.Emit(OpCodes.Ldarg_0);                                                  // Load stream
            il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamReader(typeof(string))); // Read field id
            il.Emit(OpCodes.Stloc_2);                                                  // Save last readed field id

            // Mark next field label only after field read
            if (markCurrent)
            {
              il.MarkLabel(nextFieldLabel.Value);
              nextFieldLabel = il.DefineLabel();
            }

            // Compare field id with endToken
            il.Emit(OpCodes.Ldloc_2);                               // Load readed field id
            il.Emit(OpCodes.Ldstr, SerializerTypes.TypeEndToken);   // Load endToken id
            il.Emit(OpCodes.Ldc_I4, (int)StringComparison.Ordinal); // Load comparsion type
            il.Emit(OpCodes.Call, stringEquals);                    // Compare
            il.Emit(OpCodes.Brtrue, resultLabel);                   // This is the end

            // Compare field ids, if it not equals then skip read
            il.Emit(OpCodes.Ldloc_2);                               // Load readed field id
            il.Emit(OpCodes.Ldstr, binField.Id);                    // Load field id 
            il.Emit(OpCodes.Ldc_I4, (int)StringComparison.Ordinal); // Load comparsion type
            il.Emit(OpCodes.Call, stringEquals);                    // Compare
            il.Emit(OpCodes.Brfalse, nextFieldLabel.Value);         // These aren't the field your looking for

            var deserialize = typeof(BinSerializer<>)
              .MakeGenericType(binField.Type)
              .GetTypeInfo()
              .GetMethod(nameof(BinSerializer<object>.Deserialize), BindingFlags.Static | BindingFlags.Public);

            // Prepeare stack to field set
            if (type.TypeInfo.IsValueType)
              il.Emit(OpCodes.Ldloca_S, (byte)1);
            else
              il.Emit(OpCodes.Ldloc_1);

            // Read field
            il.Emit(OpCodes.Ldarg_0); // Load stream
            il.Emit(OpCodes.Call, deserialize);

            // Set field
            binField.EmitWrite(il);
          }

          // Mark jump (for last field)
          if (nextFieldLabel != null)
            il.MarkLabel(nextFieldLabel.Value);

          // Skip end
          il.Emit(OpCodes.Ldarg_0);
          il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamSkiper(typeof(string)));
        }
      }
      
      // Return result and invoke callback
      il.MarkLabel(resultLabel);

      // Invoke deserialzation callback
      if (typeof(IBinSerializable).IsAssignableFrom(type.Type))
      {
        il.Emit(OpCodes.Ldloca_S, (byte)4);        // Load result local address
        il.Emit(OpCodes.Ldloc_3);                  // Load version
        il.Emit(OpCodes.Call, typeof(DeserializationInfo).GetConstructor(new Type[] { typeof(int) }));

        il.Emit(OpCodes.Ldloc_1);                  // Load deserialized object
        il.Emit(OpCodes.Ldloc_S, (byte)4);         // Load created DeserialzationInfo
        il.Emit(OpCodes.Callvirt, onDeserialized); // Call onDeserialized
      }

      // Return result
      il.Emit(OpCodes.Ldloc_1);

      BSDebug.TraceEnd(il, "Read " + type.TypeInfo.Name);

      il.Emit(OpCodes.Ret);
    }

    [SecurityCritical]
    private static void GenerateArrayReader(ILGenerator il, TypeImpl type)
    {
      var elementType = type.TypeInfo.GetElementType();

      var addRef = typeof(RefReaderWatcher)
        .GetTypeInfo()
        .GetMethod(nameof(RefReaderWatcher.AddRef), BindingFlags.Public | BindingFlags.Static)
        .MakeGenericMethod(type.Type);

      var tryGetRef = typeof(RefReaderWatcher)
        .GetTypeInfo()
        .GetMethod(nameof(RefReaderWatcher.TryGetRef), BindingFlags.Public | BindingFlags.Static)
        .MakeGenericMethod(type.Type);

      var deserialize = typeof(BinSerializer<>)
        .MakeGenericType(elementType)
        .GetTypeInfo()
        .GetMethod(nameof(BinSerializer<object>.Deserialize), BindingFlags.Static | BindingFlags.Public);

      // array type id already was readed

      il.DeclareLocal(typeof(int)); // Ref id
      il.DeclareLocal(typeof(int)); // Array length
      il.DeclareLocal(typeof(int)); // Current index
      il.DeclareLocal(type.Type);   // Result array

      var loopLabel = il.DefineLabel();
      var resultLabel = il.DefineLabel();
      var tryLoadReferenceLabel = il.DefineLabel();

      // Read reference id
      il.Emit(OpCodes.Ldarg_0);                                               // Load stream
      il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamReader(typeof(int))); // Read ref id
      il.Emit(OpCodes.Dup);                                                   // Duplicate ref id
      il.Emit(OpCodes.Stloc_0);                                               // Set ref id to local

      // Check if result null
      il.Emit(OpCodes.Brtrue, tryLoadReferenceLabel); // Check if null was written

      // Null was written (return null)
      il.Emit(OpCodes.Ldnull);
      il.Emit(OpCodes.Stloc_3);         // Set result to null
      il.Emit(OpCodes.Br, resultLabel); // Jump to result

      // Try get readed reference
      il.MarkLabel(tryLoadReferenceLabel);
      il.Emit(OpCodes.Ldloc_0);             // Load reference id
      il.Emit(OpCodes.Ldloca_S, (byte)3);   // Load address of result
      il.Emit(OpCodes.Call, tryGetRef);
      il.Emit(OpCodes.Brtrue, resultLabel); // Jump to result if reference already exist

      // Read array length
      il.Emit(OpCodes.Ldarg_0);                                               // Load stream
      il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamReader(typeof(int))); // Read length
      il.Emit(OpCodes.Stloc_1);                                               // Set array length

      // Set current index
      il.Emit(OpCodes.Ldc_I4_0); // Load zero
      il.Emit(OpCodes.Stloc_2);  // Set current index

      // Create array
      il.Emit(OpCodes.Ldloc_1);             // Load array length
      il.Emit(OpCodes.Newarr, elementType); // Create array
      il.Emit(OpCodes.Stloc_3);             // Set array to local

      // Set ref id
      il.Emit(OpCodes.Ldloc_0); // Load reference id
      il.Emit(OpCodes.Ldloc_3); // Load result object reference
      il.Emit(OpCodes.Call, addRef);

      // Loop
      il.MarkLabel(loopLabel);

      // Check array end
      il.Emit(OpCodes.Ldloc_1); // Load length
      il.Emit(OpCodes.Ldloc_2); // Load index
      il.Emit(OpCodes.Beq, resultLabel);

      // Prepare set element
      il.Emit(OpCodes.Ldloc_3); // Load array
      il.Emit(OpCodes.Ldloc_2); // Load index

      // Read value
      il.Emit(OpCodes.Ldarg_0);           // Load stream
      il.Emit(OpCodes.Call, deserialize); // Deserialize element

      // Set element
      il.Emit(OpCodes.Stelem, elementType);

      // Inrement index
      il.Emit(OpCodes.Ldloc_2);  // Load index
      il.Emit(OpCodes.Ldc_I4_1); // Load one
      il.Emit(OpCodes.Add);      // Sum
      il.Emit(OpCodes.Stloc_2);  // Set index

      // Go to loop start
      il.Emit(OpCodes.Br, loopLabel);

      il.MarkLabel(resultLabel);
      il.Emit(OpCodes.Ldloc_3); // Load result array
      il.Emit(OpCodes.Ret);
    }

    // Used when saved version not equal to version in runned program.
    // It's slower than generated.
    private static class DynamicObjectReader<T>
    {
      private static readonly TypeImpl Type = new TypeImpl(typeof(T));
      private static readonly Dictionary<string, BinField> FieldsMap = GetFieldsMap(Type);

      [SecurityCritical]
      static DynamicObjectReader()
      {
      }

      // Called by Emit.
      [SecurityCritical]
      public static T Read(Stream stream, T instance, int version)
      {
        var minSupportedVersion = SerializerTypes.GetMinSupported(Type);
        if (version < minSupportedVersion)
          throw new InvalidDataException($"Received version less than minimum supported ({ version } < { minSupportedVersion })");

        var boxedInstance = (object)instance;
        while (true)
        {
          var token = stream.ReadString();
          if (token == SerializerTypes.TypeEndToken)
            break;

          if (!FieldsMap.TryGetValue(token, out BinField binField))
            continue;

          var value = BinSerializer<object>.Deserialize(stream);
          binField.SetValue(boxedInstance, value);
        }

        return (T)boxedInstance;
      }

      [SecurityCritical]
      private static Dictionary<string, BinField> GetFieldsMap(TypeImpl type) =>
        BinField.Get(type.Type).ToDictionary(GetFieldId, GetField);

      [SecurityCritical]
      private static string GetFieldId(BinField f) =>
        f.Id;

      [SecurityCritical]
      private static BinField GetField(BinField f) =>
        f;
    }
    #endregion
  }
}
