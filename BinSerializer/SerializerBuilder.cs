using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using ThirtyNineEighty.BinarySerializer.Types;

#if NET45
using System.Runtime.Serialization;
#endif

namespace ThirtyNineEighty.BinarySerializer
{
  delegate void Writer<in T>(Stream stream, T obj);
  delegate T Reader<out T>(Stream stream);

  static class SerializerBuilder
  {
    private static readonly ConcurrentDictionary<TypeInfo, Delegate> Writers = new ConcurrentDictionary<TypeInfo, Delegate>();
    private static readonly ConcurrentDictionary<TypeInfo, Delegate> Readers = new ConcurrentDictionary<TypeInfo, Delegate>();

    #region writer
    [SecurityCritical]
    public static Writer<T> CreateWriter<T>(Type type)
    {
      var typeInfo = type.GetTypeInfo();
      var method = Writers.GetOrAdd(typeInfo, CreateWriterMethod);
      return MethodAdapter.CastWriter<T>(method, typeInfo);
    }

    [SecuritySafeCritical]
    private static Delegate CreateWriterMethod(TypeInfo type)
    {
      var methodName = string.Format("{0}_writer", type);
      var dynamicMethod = new DynamicMethod(methodName, typeof(void), new[] { typeof(Stream), type.AsType() }, typeof(SerializerBuilder), true);

      var il = dynamicMethod.GetILGenerator();

      if (!type.IsArray)
        GenerateObjectWriter(il, type);
      else      
        GenerateArrayWriter(il, type);

      return dynamicMethod.CreateDelegate(typeof(Writer<>).MakeGenericType(type.AsType()));
    }

    [SecurityCritical]
    private static void GenerateObjectWriter(ILGenerator il, TypeInfo type)
    {
      var getRefId = typeof(RefWriterWatcher).GetTypeInfo().GetMethod("GetRefId", BindingFlags.Public | BindingFlags.Static);

      il.DeclareLocal(typeof(bool));

      var skipTypeLabel = il.DefineLabel();

      BSDebug.TraceStart(il, "Write " + type.Name);

      // Write type
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldstr, SerializerTypes.GetTypeId(type));
      il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamWriter(typeof(string)));

      if (!type.IsValueType)
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
        foreach (var field in GetFields(type))
        {
          var skipFieldLabel = il.DefineLabel();

          // If field is null then skip it
          if (!field.Info.FieldType.GetTypeInfo().IsValueType)
          {
            il.Emit(OpCodes.Ldarg_1);                 // Load object
            il.Emit(OpCodes.Ldfld, field.Info);       // Read field form object
            il.Emit(OpCodes.Brfalse, skipFieldLabel); // Skip if field is null
          }

          // Write field id
          il.Emit(OpCodes.Ldarg_0);                   // Load stream
          il.Emit(OpCodes.Ldstr, field.Attribute.Id); // Load field id
          il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamWriter(typeof(string)));

          var serialize = typeof(BinSerializer<>)
            .MakeGenericType(field.Info.FieldType)
            .GetTypeInfo()
            .GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public);

          // Write field
          il.Emit(OpCodes.Ldarg_0);           // Load stream
          il.Emit(OpCodes.Ldarg_1);           // Load object
          il.Emit(OpCodes.Ldfld, field.Info); // Read field from object
          il.Emit(OpCodes.Call, serialize);   // Write field value

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

      BSDebug.TraceEnd(il, "Write " + type.Name);

      // End
      il.Emit(OpCodes.Ret);
    }

    [SecurityCritical]
    private static void GenerateArrayWriter(ILGenerator il, TypeInfo type)
    {
      var elementType = type.GetElementType();

      var getRefId = typeof(RefWriterWatcher).GetTypeInfo().GetMethod("GetRefId", BindingFlags.Public | BindingFlags.Static);
      var getLowerBound = typeof(Array).GetTypeInfo().GetMethod("GetLowerBound", BindingFlags.Instance | BindingFlags.Public);
      var serialize = typeof(BinSerializer<>).MakeGenericType(elementType).GetTypeInfo().GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public);

      il.DeclareLocal(typeof(int));  // Array length
      il.DeclareLocal(typeof(int));  // Array index
      il.DeclareLocal(typeof(bool)); // Ref id created flag

      var skipLabel = il.DefineLabel();
      var zeroBased = il.DefineLabel();
      var loopLabel = il.DefineLabel();
      var exitLoopLabel = il.DefineLabel();

      BSDebug.TraceStart(il, "Write " + type.Name);

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

      var dim = type.GetArrayRank();
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

      BSDebug.TraceEnd(il, "Write " + type.FullName);

      // End
      il.Emit(OpCodes.Ret);
    }
    #endregion

    #region reader
    [SecurityCritical]
    public static Reader<T> CreateReader<T>(Type type)
    {
      var typeInfo = type.GetTypeInfo();
      var method = Readers.GetOrAdd(typeInfo, CreateReaderMethod);
      return MethodAdapter.CastReader<T>(method, typeInfo);
    }

    [SecurityCritical]
    private static Delegate CreateReaderMethod(TypeInfo type)
    {
      var methodName = string.Format("{0}_reader", type);
      var dynamicMethod = new DynamicMethod(methodName, type, new[] { typeof(Stream) }, typeof(SerializerBuilder), true);

      var il = dynamicMethod.GetILGenerator();

      if (!type.IsArray)
        GenerateObjectReader(il, type);
      else
        GenerateArrayReader(il, type);

      return dynamicMethod.CreateDelegate(typeof(Reader<>).MakeGenericType(type.AsType()));
    }

    [SecurityCritical]
    private static void GenerateObjectReader(ILGenerator il, TypeInfo type)
    {
      var addRef = typeof(RefReaderWatcher).GetTypeInfo().GetMethod("AddRef", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(type.AsType());
      var tryGetRef = typeof(RefReaderWatcher).GetTypeInfo().GetMethod("TryGetRef", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(type.AsType());
      var getTypeFromHandle = typeof(Type).GetTypeInfo().GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
      var defaultCtor = type.GetConstructor(Type.EmptyTypes);
      var getUninitializedObject = typeof(RuntimeHelpers).GetTypeInfo().GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);
      var dynamicObjectRead = typeof(DynamicObjectReader<>).MakeGenericType(type.AsType()).GetTypeInfo().GetMethod("Read", BindingFlags.Public | BindingFlags.Static);
      var stringEquals = typeof(string).GetTypeInfo().GetMethod("Equals", new[] { typeof(string), typeof(string), typeof(StringComparison) });

      il.DeclareLocal(typeof(int));    // ref if
      il.DeclareLocal(type);           // readed object
      il.DeclareLocal(typeof(string)); // last readed field id
      il.DeclareLocal(typeof(int));    // readed object version

      var resultLabel = il.DefineLabel();
      var readFastLabel = il.DefineLabel();

      // Process reference id
      if (!type.IsValueType)
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
        if (!type.IsValueType)
        {
          il.Emit(OpCodes.Ldloc_0); // Load reference id
          il.Emit(OpCodes.Ldloc_1); // Load result object reference
          il.Emit(OpCodes.Call, addRef);
        }
      }
      else
      {
        // Create
        if (!type.IsValueType)
        {
          if (defaultCtor != null)
          {
            il.Emit(OpCodes.Newobj, defaultCtor);
            il.Emit(OpCodes.Stloc_1); // Set result to local
          }
          else
          {
            il.Emit(OpCodes.Ldtoken, type);
            il.Emit(OpCodes.Call, getTypeFromHandle);
            il.Emit(OpCodes.Call, getUninitializedObject);
            il.Emit(OpCodes.Castclass, type);
            il.Emit(OpCodes.Stloc_1); // Set result to local
          }
        }
        else
        {
          il.Emit(OpCodes.Ldloca_S, (byte)1); // Load result local address
          il.Emit(OpCodes.Initobj, type);
        }

        // Add reference to watcher
        if (!type.IsValueType)
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

          foreach (var field in GetFields(type))
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

            // Compare field ids, if it not equals then skip read
            il.Emit(OpCodes.Ldloc_2);                               // Load readed field id
            il.Emit(OpCodes.Ldstr, field.Attribute.Id);             // Load field id 
            il.Emit(OpCodes.Ldc_I4, (int)StringComparison.Ordinal); // Load comparsion type
            il.Emit(OpCodes.Call, stringEquals);                    // Compare
            il.Emit(OpCodes.Brfalse, nextFieldLabel.Value);         // These aren't the field your looking for

            var deserialize = typeof(BinSerializer<>)
              .MakeGenericType(field.Info.FieldType)
              .GetTypeInfo()
              .GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public);

            // Prepeare stack to field set
            if (type.IsValueType)
              il.Emit(OpCodes.Ldloca_S, (byte)1);
            else
              il.Emit(OpCodes.Ldloc_1);

            // Read field
            il.Emit(OpCodes.Ldarg_0); // Load stream
            il.Emit(OpCodes.Call, deserialize);

            // Set field
            il.Emit(OpCodes.Stfld, field.Info);
          }

          // Mark jump (for last field)
          if (nextFieldLabel != null)
            il.MarkLabel(nextFieldLabel.Value);

          // Skip end
          il.Emit(OpCodes.Ldarg_0);
          il.Emit(OpCodes.Call, SerializerTypes.TryGetStreamSkiper(typeof(string)));
        }
      }

      // Return result
      il.MarkLabel(resultLabel);
      il.Emit(OpCodes.Ldloc_1);

      BSDebug.TraceEnd(il, "Read " + type.Name);

      il.Emit(OpCodes.Ret);
    }

    [SecurityCritical]
    private static void GenerateArrayReader(ILGenerator il, Type type)
    {
      var elementType = type.GetElementType();

      var addRef = typeof(RefReaderWatcher).GetTypeInfo().GetMethod("AddRef", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(type);
      var tryGetRef = typeof(RefReaderWatcher).GetTypeInfo().GetMethod("TryGetRef", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(type);
      var deserialize = typeof(BinSerializer<>).MakeGenericType(elementType).GetTypeInfo().GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public);

      // array type id already was readed

      il.DeclareLocal(typeof(int)); // Ref id
      il.DeclareLocal(typeof(int)); // Array length
      il.DeclareLocal(typeof(int)); // Current index
      il.DeclareLocal(type);        // Result array

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
      private static readonly TypeInfo Type = typeof(T).GetTypeInfo();
      private static readonly Dictionary<string, FieldInfo> FieldsMap = GetFieldsMap(Type);

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
          throw new InvalidDataException(string.Format("Received version less than minimum supported ({0} < {1})", version, minSupportedVersion));

        var boxedInstance = (object)instance;
        while (true)
        {
          var token = stream.ReadString();
          if (token == SerializerTypes.TypeEndToken)
            break;

          FieldInfo field;
          if (!FieldsMap.TryGetValue(token, out field))
            continue;

          field.SetValue(boxedInstance, BinSerializer<object>.Deserialize(stream));
        }

        return (T)boxedInstance;
      }

      [SecurityCritical]
      private static Dictionary<string, FieldInfo> GetFieldsMap(TypeInfo type)
      {
        return GetFields(type).ToDictionary(f => f.Attribute.Id, f => f.Info);
      }
    }
    #endregion

    #region get fields
    private struct Field
    {
      public readonly BinFieldAttribute Attribute;
      public readonly FieldInfo Info;

      public Field(BinFieldAttribute attribute, FieldInfo fieldInfo)
      {
        Attribute = attribute;
        Info = fieldInfo;
      }
    }

    [SecurityCritical]
    private static IEnumerable<Field> GetFields(TypeInfo type)
    {
      var fields = type
        .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Select(f => new Field(f.GetCustomAttribute<BinFieldAttribute>(false), f))
        .Where(f => f.Attribute != null);

      var currentType = type;
      while (true)
      {
        currentType = currentType.BaseType.GetTypeInfo();
        if (typeof(object) == currentType.AsType())
          break;

        var baseTypePrivateFields = currentType
          .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
          .Where(f => !f.IsFamily)
          .Select(f => new Field(f.GetCustomAttribute<BinFieldAttribute>(false), f))
          .Where(f => f.Attribute != null);

        fields = fields.Concat(baseTypePrivateFields);
      }

      var declaredIds = new HashSet<string>();
      foreach (var field in fields.OrderBy(p => p.Attribute.Id))
      {
        if (!declaredIds.Add(field.Attribute.Id))
          throw new ArgumentException(string.Format("Field \"{0}\" declared twice in {1} type", field.Attribute.Id, field.Info.DeclaringType));
        if (field.Info.IsInitOnly)
          throw new ArgumentException(string.Format("Field {0} can't be readonly (IsInitOnly = true). For type {1}", field.Info.Name, field.Info.DeclaringType));
        yield return field;
      }
    }
    #endregion
  }
}
