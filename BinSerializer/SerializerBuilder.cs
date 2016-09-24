using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace ThirtyNineEighty.BinarySerializer
{
  delegate void Writer(Stream stream, object obj);
  delegate void Writer<in T>(Stream stream, T obj);

  delegate object Reader(Stream stream);
  delegate T Reader<out T>(Stream stream);

  /* Serialization format:
   * 
   * <=============================================>
   * <==============SIMPLE VALUE TYPE==============>
   * <=============================================>
   * 
   * |-------------------------------------|
   * | string - type Name                  | 
   * |-------------------------------------|
   * | int - written type version          |
   * |-------------------------------------|
   * | bytes - type data                   | 
   * |-------------------------------------|
   * 
   * <=============================================>
   * <==============SIMPLE REF TYPE================>
   * <=============================================>
   * 
   * |-------------------------------------|
   * | string - type Name                  | 
   * |-------------------------------------|
   * |                                     | 0 for null.
   * | int - reference id                  | If reference is null it's be end of type.
   * |                                     | If reference already exist when you read then it is also be end of type.
   * |-------------------------------------|
   * | int - written type version          |
   * |-------------------------------------|
   * | bytes - type data                   |
   * |-------------------------------------|
   * 
   * <=============================================>
   * <==============USER DEFINED VALUE TYPE========>
   * <=============================================>
   * 
   * |-------------------------------------|
   * | string - type Name                  | 
   * |-------------------------------------|
   * | int - written type version          |
   * |-------------------------------------|
   * 
   * // Fields (if field is null nothing will be written)
   * |-------------------------------------|
   * | string - field id                   |
   * |-------------------------------------|
   * | inner type (starts from header)     |
   * |-------------------------------------|
   * 
   * // Other fields
   * // ...
   * 
   * // Last field
   * |-------------------------------------|
   * | string - field id                   |
   * |-------------------------------------|
   * | inner type (starts form header)     |
   * |-------------------------------------|
   * 
   * // End
   * |-------------------------------------|
   * | string = TypeEndToken - end of type | 
   * |-------------------------------------|
   * 
   * <=============================================>
   * <==============USER DEFINED REF TYPE==========>
   * <=============================================>
   * 
   * |-------------------------------------|
   * | string - type Name                  | 
   * |-------------------------------------|
   * |                                     | 0 for null. 
   * | int - reference id                  | If reference is null it's be end of type.
   * |                                     | If reference already exist when you read then it is also be end of type.
   * |-------------------------------------|
   * | int - written type version          |
   * |-------------------------------------|
   * 
   * // Fields (if field is null nothing will be written)
   * |-------------------------------------|
   * | string - field id                   |
   * |-------------------------------------|
   * | inner type (starts from header)     |
   * |-------------------------------------|
   * 
   * // Other fields
   * // ...
   * 
   * // Last field
   * |-------------------------------------|
   * | string - field id                   |
   * |-------------------------------------|
   * | inner type (starts form header)     |
   * |-------------------------------------|
   * 
   * // End
   * |-------------------------------------|
   * | string = TypeEndToken - end of type | 
   * |-------------------------------------|
   * 
   * <=============================================>
   * <==============ARRAY==========================>
   * <=============================================>
   * 
   * |-------------------------------------|
   * | string = ArrayToken[elementTypeId]  |
   * |-------------------------------------|
   * |                                     | 0 for null. 
   * | int - reference id                  | If reference is null it's be end of type.
   * |                                     | If reference already exist when you read then it is also be end of type.
   * |-------------------------------------|
   * | int - array length                  |
   * |-------------------------------------|
   * 
   * // Array elements
   * |-------------------------------------|
   * | inner type (starts from header)     |
   * |-------------------------------------|
   * 
   * // Other elements
   * // ...
   * 
   * // Last element
   * |-------------------------------------|
   * | inner type (starts form header)     |
   * |-------------------------------------|
   * */

  static class SerializerBuilder
  {
    private static readonly ConcurrentDictionary<Type, DynamicMethod> _writersD = new ConcurrentDictionary<Type, DynamicMethod>();
    private static readonly ConcurrentDictionary<Type, DynamicMethod> _readersD = new ConcurrentDictionary<Type, DynamicMethod>();

    private static readonly ConcurrentDictionary<Type, Writer> _writers = new ConcurrentDictionary<Type, Writer>();
    private static readonly ConcurrentDictionary<Type, Reader> _readers = new ConcurrentDictionary<Type, Reader>();

    private static readonly ConcurrentDictionary<Type, Delegate> _writersT = new ConcurrentDictionary<Type, Delegate>();
    private static readonly ConcurrentDictionary<Type, Delegate> _readersT = new ConcurrentDictionary<Type, Delegate>();

    #region writer
    // Only for refs
    public static Writer GetWriter(Type type)
    {
      return _writers.GetOrAdd(type, t =>
      {
        var dynamicMethod = _writersD.GetOrAdd(t, CreateWriter);
        return (Writer)dynamicMethod.CreateDelegate(typeof(Writer));
      });
    }

    // Only for structs
    public static Writer<T> GetWriter<T>()
    {
      return (Writer<T>)_writersT.GetOrAdd(typeof(T), t =>
      {
        var dynamicMethod = _writersD.GetOrAdd(t, CreateWriter);
        return dynamicMethod.CreateDelegate(typeof(Writer<T>));
      });
    }

    private static DynamicMethod CreateWriter(Type type)
    {
      DynamicMethod dynamicMethod = null;

      var methodName = string.Format("{0}_writer", type);
      if (type.IsValueType)
        dynamicMethod = new DynamicMethod(methodName, typeof(void), new[] { typeof(Stream), type }, type, true);
      if (!type.IsValueType && !type.IsArray)
        dynamicMethod = new DynamicMethod(methodName, typeof(void), new[] { typeof(Stream), typeof(object) }, type, true);
      if (type.IsArray)
        dynamicMethod = new DynamicMethod(methodName, typeof(void), new[] { typeof(Stream), typeof(object) }, type.GetElementType(), true);

      if (dynamicMethod == null)
        throw new NotImplementedException(string.Format("DynamicMethod not builded for type: {0}", type));

      var il = dynamicMethod.GetILGenerator();

      if (!type.IsArray)
        CreateObjectWriter(il, type);
      else      
        CreateArrayWriter(il, type);

      return dynamicMethod;
    }

    private static void CreateObjectWriter(ILGenerator il, Type type)
    {
      var getRefId = typeof(RefWriterWatcher).GetMethod("GetRefId", BindingFlags.Public | BindingFlags.Static);

      il.DeclareLocal(typeof(bool));

      var skipTypeLabel = il.DefineLabel();

      BSDebug.TraceStart(il, "Write " + type.Name);

      // Write type
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldstr, Types.GetTypeId(type));
      il.Emit(OpCodes.Call, Types.TryGetWriter(typeof(string)));

      if (!type.IsValueType)
      {
        // Write refId
        il.Emit(OpCodes.Ldarg_0);         // Load stream
        il.Emit(OpCodes.Ldarg_1);         // Load obj
        il.Emit(OpCodes.Ldloca_S, 0);     // Load address of bool local
        il.Emit(OpCodes.Call, getRefId);  // Obj => RefId
        il.Emit(OpCodes.Call, Types.TryGetWriter(typeof(int)));

        // Check case when reference type already serialized.
        // If null be returned then created flag be zero too and serialization will be skipped.
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Brfalse, skipTypeLabel);
      }

      // Write type version
      var version = Types.GetVersion(type);

      il.Emit(OpCodes.Ldarg_0);         // Load stream
      il.Emit(OpCodes.Ldc_I4, version); // Load version
      il.Emit(OpCodes.Call, Types.TryGetWriter(typeof(int)));

      // If writer exist then just write
      var writer = Types.TryGetWriter(type);
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
          var fieldAttribute = field.GetCustomAttribute<FieldAttribute>(false);

          // If field is null then skip it
          if (!field.FieldType.IsValueType)
          {
            il.Emit(OpCodes.Ldarg_1);                 // Load object
            il.Emit(OpCodes.Ldfld, field);            // Read field form object
            il.Emit(OpCodes.Brfalse, skipFieldLabel); // Skip if field is null
          }

          // Write field id
          il.Emit(OpCodes.Ldarg_0);
          il.Emit(OpCodes.Ldstr, fieldAttribute.Id);
          il.Emit(OpCodes.Call, Types.TryGetWriter(typeof(string)));

          var serialize = typeof(BinSerializer)
            .GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public)
            .MakeGenericMethod(field.FieldType);

          // Write field
          il.Emit(OpCodes.Ldarg_0);         // Load stream
          il.Emit(OpCodes.Ldarg_1);         // Load object
          il.Emit(OpCodes.Ldfld, field);    // Read field from object
          il.Emit(OpCodes.Call, serialize); // Write field value

          // Skip field label
          il.MarkLabel(skipFieldLabel);
        }

        // Write end id
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldstr, Types.TypeEndToken);
        il.Emit(OpCodes.Call, Types.TryGetWriter(typeof(string))); 
      }

      // Skip type label
      il.MarkLabel(skipTypeLabel);

      BSDebug.TraceEnd(il, "Write " + type.Name);

      // End
      il.Emit(OpCodes.Ret);
    }

    private static void CreateArrayWriter(ILGenerator il, Type type)
    {
      var elementType = type.GetElementType();

      var getRefId = typeof(RefWriterWatcher).GetMethod("GetRefId", BindingFlags.Public | BindingFlags.Static);
      var getLowerBound = typeof(Array).GetMethod("GetLowerBound", BindingFlags.Instance | BindingFlags.Public);
      var serialize = typeof(BinSerializer).GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(elementType);

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
      il.Emit(OpCodes.Ldstr, Types.GetTypeId(type));
      il.Emit(OpCodes.Call, Types.TryGetWriter(typeof(string)));

      // Write refId
      il.Emit(OpCodes.Ldarg_0);         // Load stream
      il.Emit(OpCodes.Ldarg_1);         // Load obj
      il.Emit(OpCodes.Ldloca_S, 2);     // Load address of bool local
      il.Emit(OpCodes.Call, getRefId);  // Obj => RefId
      il.Emit(OpCodes.Call, Types.TryGetWriter(typeof(int)));

      // Check case when reference type already serialized.
      // If null be returned then created flag be zero too and serialization will be skipped.
      il.Emit(OpCodes.Ldloc_2);
      il.Emit(OpCodes.Brfalse, skipLabel);

      var dim = type.GetArrayRank();
      if (dim == 1)
      {
        // Dimension check
        il.Emit(OpCodes.Ldarg_0);                 // Load array
        il.Emit(OpCodes.Ldc_I4_0);                // Load zero (0 dim)
        il.Emit(OpCodes.Callvirt, getLowerBound); // Get lower bound of 0 dim
        il.Emit(OpCodes.Brfalse, zeroBased);      // If result 0 then jump to zeroBased

        // Throw exception if non zero based
        il.Emit(OpCodes.Ldstr, "Non zero based arrays not supported.");
        il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }));
        il.Emit(OpCodes.Throw);

        // Write array length
        il.MarkLabel(zeroBased);
        il.Emit(OpCodes.Ldarg_0); // Load stream
        il.Emit(OpCodes.Ldarg_1); // Load array
        il.Emit(OpCodes.Ldlen);   // Load array length
        il.Emit(OpCodes.Call, Types.TryGetWriter(typeof(int)));

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
    // Only for refs
    public static Reader GetReader(Type type)
    {
      return _readers.GetOrAdd(type, t =>
      {
        var dynamicMethod = _readersD.GetOrAdd(t, CreateReader);
        return (Reader)dynamicMethod.CreateDelegate(typeof(Reader));
      });
    }

    // Only for structs
    public static Reader<T> GetReader<T>()
    {
      return (Reader<T>)_readersT.GetOrAdd(typeof(T), t =>
      {
        var dynamicMethod = _readersD.GetOrAdd(t, CreateReader);
        return dynamicMethod.CreateDelegate(typeof(Reader<T>));
      });
    }

    private static DynamicMethod CreateReader(Type type)
    {
      DynamicMethod dynamicMethod = null;

      var methodName = string.Format("{0}_reader", type);
      if (type.IsValueType)
        dynamicMethod = new DynamicMethod(methodName, type, new[] { typeof(Stream) }, type, true);
      if (!type.IsValueType && !type.IsArray)
        dynamicMethod = new DynamicMethod(methodName, typeof(object), new[] { typeof(Stream) }, type, true);
      if (type.IsArray)
        dynamicMethod = new DynamicMethod(methodName, typeof(object), new[] { typeof(Stream) }, type.GetElementType(), true);

      if (dynamicMethod == null)
        throw new NotImplementedException(string.Format("DynamicMethod not builded for type: {0}", type));

      var il = dynamicMethod.GetILGenerator();

      if (!type.IsArray)
        CreateObjectReader(il, type);
      else
        CreateArrayReader(il, type);

      return dynamicMethod;
    }

    private static void CreateObjectReader(ILGenerator il, Type type)
    {
      var addRef = typeof(RefReaderWatcher).GetMethod("AddRef", BindingFlags.Public | BindingFlags.Static);
      var tryGetRef = typeof(RefReaderWatcher).GetMethod("TryGetRef", BindingFlags.Public | BindingFlags.Static);
      var getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
      var getUninitializedObject = typeof(FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);
      var dynamicObjectRead = typeof(SerializerBuilder).GetMethod("DynamicObjectRead", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(type);
      var stringEquals = typeof(string).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string), typeof(StringComparison) }, null);

      il.DeclareLocal(typeof(int));
      il.DeclareLocal(type);
      il.DeclareLocal(typeof(string));

      var resultLabel = il.DefineLabel();
      var readVersionLabel = il.DefineLabel();
      var readFastLabel = il.DefineLabel();

      // Type readed before only for references
      if (type.IsValueType)
      {
        // Skip type for value types
        il.Emit(OpCodes.Ldarg_0);                                  // Load stream
        il.Emit(OpCodes.Call, Types.TryGetSkiper(typeof(string))); // Skip type
      }

      // Process reference id
      if (!type.IsValueType)
      {
        var tryLoadReferenceLabel = il.DefineLabel();

        // Read reference id
        il.Emit(OpCodes.Ldarg_0);                               // Load stream
        il.Emit(OpCodes.Call, Types.TryGetReader(typeof(int))); // Read ref id
        il.Emit(OpCodes.Dup);                                   // Duplicate ref id
        il.Emit(OpCodes.Stloc_0);                               // Set ref id to local

        // Check if result null
        il.Emit(OpCodes.Brtrue, tryLoadReferenceLabel);     // Check if null was written

        // Null was written (return null)
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Stloc_1);         // Set result to null
        il.Emit(OpCodes.Br, resultLabel); // Jump to result

        // Try get readed reference
        il.MarkLabel(tryLoadReferenceLabel);
        il.Emit(OpCodes.Ldloc_0);             // Load reference id
        il.Emit(OpCodes.Ldloca_S, 1);         // Load address of result
        il.Emit(OpCodes.Call, tryGetRef);
        il.Emit(OpCodes.Brtrue, resultLabel); // Jump to result if reference already exist
      }

      il.MarkLabel(readVersionLabel);

      // Read code type version
      var version = Types.GetVersion(type);

      // Compare versions
      il.Emit(OpCodes.Ldc_I4, version);                       // Load program type version 
      il.Emit(OpCodes.Ldarg_0);                               // Load stream
      il.Emit(OpCodes.Call, Types.TryGetReader(typeof(int))); // Read saved version
      il.Emit(OpCodes.Beq, readFastLabel);

      // Call dynamic read if versions not equal
      il.Emit(OpCodes.Ldarg_0);                 // Load stream
      il.Emit(OpCodes.Call, dynamicObjectRead); // Do dynamic read
      il.Emit(OpCodes.Stloc_1);                 // Set readed value to result local
      il.Emit(OpCodes.Br, resultLabel);         // Jump to return code part

      il.MarkLabel(readFastLabel);

      var reader = Types.TryGetReader(type);
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
          il.Emit(OpCodes.Ldtoken, type);
          il.Emit(OpCodes.Call, getTypeFromHandle);
          il.Emit(OpCodes.Call, getUninitializedObject);
          il.Emit(OpCodes.Stloc_1); // Set result to local
        }
        else
        {
          il.Emit(OpCodes.Ldloca_S, 1); // Load result local address
          il.Emit(OpCodes.Initobj, type);
        }

        // Add reference to watcher
        if (!type.IsValueType)
        {
          il.Emit(OpCodes.Ldloc_0); // Load reference id
          il.Emit(OpCodes.Ldloc_1); // Load result object reference
          il.Emit(OpCodes.Call, addRef);
        }

        // Read fields
        Label? nextFieldLabel = null;

        foreach (var field in GetFields(type))
        {
          var fieldAttribute = field.GetCustomAttribute<FieldAttribute>(false);

          var markCurrent = nextFieldLabel != null;
          if (nextFieldLabel == null)
            nextFieldLabel = il.DefineLabel();

          // Read field id
          il.Emit(OpCodes.Ldarg_0);                                  // Load stream
          il.Emit(OpCodes.Call, Types.TryGetReader(typeof(string))); // Read field id
          il.Emit(OpCodes.Stloc_2);                                  // Save last readed field id

          // Mark next field label only after field read
          if (markCurrent)
          {
            il.MarkLabel(nextFieldLabel.Value);
            nextFieldLabel = il.DefineLabel();
          }

          // Compare field ids, if it not equals then skip read
          il.Emit(OpCodes.Ldloc_2);                                        // Load readed field id
          il.Emit(OpCodes.Ldstr, fieldAttribute.Id);                       // Load field id 
          il.Emit(OpCodes.Ldc_I4, (int)StringComparison.InvariantCulture); // Load comparsion type
          il.Emit(OpCodes.Call, stringEquals);                             // Compare
          il.Emit(OpCodes.Brfalse, nextFieldLabel.Value);                  // These aren't the field your looking for

          var deserialize = typeof(BinSerializer)
            .GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public)
            .MakeGenericMethod(field.FieldType);

          // Prepeare stack to field set
          if (!type.IsValueType)
            il.Emit(OpCodes.Ldloc_1);
          else
            il.Emit(OpCodes.Ldloca_S, 1); 

          // Read field
          il.Emit(OpCodes.Ldarg_0); // Load stream
          il.Emit(OpCodes.Call, deserialize);
          
          // Set field
          il.Emit(OpCodes.Stfld, field);
        }

        // Mark jump (for last field)
        if (nextFieldLabel != null)
          il.MarkLabel(nextFieldLabel.Value);

        // Skip end
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, Types.TryGetSkiper(typeof(string)));
      }

      // Return result
      il.MarkLabel(resultLabel);
      il.Emit(OpCodes.Ldloc_1);

      BSDebug.TraceEnd(il, "Read " + type.Name);

      il.Emit(OpCodes.Ret);
    }

    private static void CreateArrayReader(ILGenerator il, Type type)
    {
      var elementType = type.GetElementType();

      var addRef = typeof(RefReaderWatcher).GetMethod("AddRef", BindingFlags.Public | BindingFlags.Static);
      var tryGetRef = typeof(RefReaderWatcher).GetMethod("TryGetRef", BindingFlags.Public | BindingFlags.Static);
      var deserialize = typeof(BinSerializer).GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(elementType);

      // array type id already was readed

      il.DeclareLocal(typeof(int)); // Ref id
      il.DeclareLocal(typeof(int)); // Array length
      il.DeclareLocal(typeof(int)); // Current index
      il.DeclareLocal(type);        // Result array

      var loopLabel = il.DefineLabel();
      var resultLabel = il.DefineLabel();
      var tryLoadReferenceLabel = il.DefineLabel();

      // Read reference id
      il.Emit(OpCodes.Ldarg_0);                               // Load stream
      il.Emit(OpCodes.Call, Types.TryGetReader(typeof(int))); // Read ref id
      il.Emit(OpCodes.Dup);                                   // Duplicate ref id
      il.Emit(OpCodes.Stloc_0);                               // Set ref id to local

      // Check if result null
      il.Emit(OpCodes.Brtrue, tryLoadReferenceLabel); // Check if null was written

      // Null was written (return null)
      il.Emit(OpCodes.Ldnull);
      il.Emit(OpCodes.Stloc_3);         // Set result to null
      il.Emit(OpCodes.Br, resultLabel); // Jump to result

      // Try get readed reference
      il.MarkLabel(tryLoadReferenceLabel);
      il.Emit(OpCodes.Ldloc_0);             // Load reference id
      il.Emit(OpCodes.Ldloca_S, 3);         // Load address of result
      il.Emit(OpCodes.Call, tryGetRef);
      il.Emit(OpCodes.Brtrue, resultLabel); // Jump to result if reference already exist

      // Read array length
      il.Emit(OpCodes.Ldarg_0);                               // Load stream
      il.Emit(OpCodes.Call, Types.TryGetReader(typeof(int))); // Read length
      il.Emit(OpCodes.Stloc_1);                               // Set array length

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
    private static T DynamicObjectRead<T>(Stream stream)
    {
      throw new NotImplementedException();
    }
    #endregion

    #region get fields
    private static IEnumerable<FieldInfo> GetFields(Type type)
    {
      return type
        .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Select(f => new { Field = f, Attribute = f.GetCustomAttribute<FieldAttribute>(false) })
        .Where(i => i.Attribute != null)
        .OrderBy(i => i.Attribute.Id)
        .Select(i => i.Field);
    }
    #endregion
  }
}
