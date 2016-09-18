using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace ThirtyNineEighty.BinSerializer
{
  public delegate void Writer(Stream stream, object obj);
  public delegate void Writer<T>(Stream stream, T obj);

  public delegate object Reader(Stream stream);
  public delegate T Reader<T>(Stream stream);

  /* Serialization format:
   * 
   * // Header
   * |-------------------------------------| Type Id
   * | byte - format type                  |
   * ||-------------|---------------------|| 
   * ||int - typeId |string - type Name   || 2 formats
   * ||-------------|---------------------||
   * |-------------------------------------|
   * |                                     | Block only for references (0 for null).  
   * |int - reference id                   | If reference is null it's be end of type.
   * |                                     | If reference already exist when you read then it is also be end of type.
   * |-------------------------------------|
   * |version - writed type version        | Block for complex types
   * |-------------------------------------|
   * |bytes - type data                    | Block for simple types (defined in _fieldWriters)
   * |-------------------------------------|
   * 
   * // Fields
   * |-------------------------------------|
   * |int - field id                       | Field blocks for complex types
   * |-------------------------------------|
   * |inner type (starts from header)      |
   * |-------------------------------------|
   * 
   * // Other fields
   * // ...
   * 
   * // Last field
   * |-------------------------------------|
   * |int - field id                       | Field blocks for complex types
   * |-------------------------------------|
   * |inner type (starts form header)      |
   * |-------------------------------------|
   * 
   * // End
   * |-------------------------------------|
   * |int = 0 - end of type                | Field id with negative id and with zero id is forbidden
   * |-------------------------------------|
   * */
  
  // Support .net serialization features like ISerializable, IDeserializationCallback, OnSerializing, OnSerialized, OnDeserializing, OnDeserialied
  public static class SerializerBuilder
  {
    private static readonly ConcurrentDictionary<Type, DynamicMethod> _writersD = new ConcurrentDictionary<Type, DynamicMethod>();
    private static readonly ConcurrentDictionary<Type, DynamicMethod> _readersD = new ConcurrentDictionary<Type, DynamicMethod>();

    private static readonly ConcurrentDictionary<Type, Writer> _writers = new ConcurrentDictionary<Type, Writer>();
    private static readonly ConcurrentDictionary<Type, Reader> _readers = new ConcurrentDictionary<Type, Reader>();

    private static readonly ConcurrentDictionary<Type, Delegate> _writersT = new ConcurrentDictionary<Type, Delegate>();
    private static readonly ConcurrentDictionary<Type, Delegate> _readersT = new ConcurrentDictionary<Type, Delegate>();

    private static readonly Dictionary<Type, MethodInfo> _streamWriters = new Dictionary<Type, MethodInfo>();
    private static readonly Dictionary<Type, MethodInfo> _streamReaders = new Dictionary<Type, MethodInfo>();
    private static readonly Dictionary<Type, MethodInfo> _streamSkipers = new Dictionary<Type, MethodInfo>();

    static SerializerBuilder()
    {
      foreach (var method in typeof(StreamExtensions).GetMethods())
      {
        var attrib = method.GetCustomAttribute<StreamExtensionAttribute>(false);
        if (attrib == null)
          continue;

        switch (attrib.Kind)
        {
          case StreamExtensionKind.Read:  _streamReaders.Add(attrib.Type, method); break;
          case StreamExtensionKind.Write: _streamWriters.Add(attrib.Type, method); break;
          case StreamExtensionKind.Skip:  _streamSkipers.Add(attrib.Type, method); break;
        }
      }
    }

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

      if (type.IsValueType)
        dynamicMethod = new DynamicMethod(string.Format("{0}_writer", type), typeof(void), new Type[] { typeof(Stream), type }, type, true);
      if (type.IsClass && !type.IsArray)
        dynamicMethod = new DynamicMethod(string.Format("{0}_writer", type), typeof(void), new Type[] { typeof(Stream), typeof(object) }, type, true);
      if (type.IsArray)
        dynamicMethod = new DynamicMethod(string.Format("{0}_writer", type), typeof(void), new Type[] { typeof(Stream), typeof(object) }, type.GetElementType(), true);

      if (dynamicMethod == null)
        throw new NotImplementedException($"DynamicMethod not bilded for type: { type }");

      var il = dynamicMethod.GetILGenerator();

      if (!type.IsArray)
        CreateObjectWriter(il, type);
      else      
        CreateArrayWriter(il, type);

      return dynamicMethod;
    }

    private static void CreateObjectWriter(ILGenerator il, Type type)
    {
      var getRefId = typeof(RefWriterWatcher).GetMethod(nameof(RefWriterWatcher.GetRefId), BindingFlags.Public | BindingFlags.Static);
      var getTypeFromHadle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Public | BindingFlags.Static);

      il.DeclareLocal(typeof(bool));

      var skipLabel = il.DefineLabel();

      BSDebug.TraceStart(il, "Write " + type.Name);

      // Write type
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldtoken, type);
      il.Emit(OpCodes.Call, getTypeFromHadle);
      il.Emit(OpCodes.Call, _streamWriters[typeof(Type)]);

      if (type.IsClass)
      {
        // Write refId
        il.Emit(OpCodes.Ldarg_0);         // Load stream
        il.Emit(OpCodes.Ldarg_1);         // Load obj
        il.Emit(OpCodes.Ldloca_S, 0);     // Load address of bool local
        il.Emit(OpCodes.Call, getRefId);  // Obj => RefId
        il.Emit(OpCodes.Call, _streamWriters[typeof(int)]);

        // Check case when reference type already serialized.
        // If null be returned then created flag be zero too and serialization will be skipped.
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Brfalse, skipLabel);
      }

      // If writer exist then just write
      MethodInfo writer;
      if (_streamWriters.TryGetValue(type, out writer))
      {
        il.Emit(OpCodes.Ldarg_0);      // Load stream
        il.Emit(OpCodes.Ldarg_1);      // Load obj
        il.Emit(OpCodes.Call, writer); // Write to stream
      }
      else
      {
        // Write type version
        int version;
        if (!Types.TryGetVersion(type, out version))
          version = 0;

        il.Emit(OpCodes.Ldarg_0);         // Load stream
        il.Emit(OpCodes.Ldc_I4, version); // Load version
        il.Emit(OpCodes.Call, _streamWriters[typeof(int)]);
        
        foreach (var field in GetFields(type))
        {
          // Write field id
          var fieldAttribute = field.GetCustomAttribute<FieldAttribute>(false);
          if (fieldAttribute != null)
          {
            // Write field id format (0 for id)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, _streamWriters[typeof(int)]);

            // Write field id
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, fieldAttribute.Id);
            il.Emit(OpCodes.Call, _streamWriters[typeof(int)]);
          }
          else
          {
            // Write field id format (1 for field name)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, _streamWriters[typeof(int)]);

            // Write field name
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, field.Name);
            il.Emit(OpCodes.Call, _streamWriters[typeof(string)]);
          }

          var serialize = typeof(BinSerializer)
            .GetMethod(nameof(BinSerializer.Serialize), BindingFlags.Static | BindingFlags.Public)
            .MakeGenericMethod(field.FieldType);

          // Write field
          il.Emit(OpCodes.Ldarg_0);
          il.Emit(OpCodes.Ldarg_1);
          il.Emit(OpCodes.Ldfld, field);
          il.Emit(OpCodes.Call, serialize);
        }

        // Write end (format)
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Call, _streamWriters[typeof(int)]);

        // Write end (value)
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Call, _streamWriters[typeof(int)]);
      }

      // Skipped
      il.MarkLabel(skipLabel);

      BSDebug.TraceEnd(il, "Write " + type.Name);

      // End
      il.Emit(OpCodes.Ret);
    }

    private static void CreateArrayWriter(ILGenerator il, Type type)
    {
      var elementType = type.GetElementType();

      var getRefId = typeof(RefWriterWatcher).GetMethod(nameof(RefWriterWatcher.GetRefId), BindingFlags.Public | BindingFlags.Static);
      var fetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Public | BindingFlags.Static);
      var getLowerBound = typeof(Array).GetMethod(nameof(Array.GetLowerBound), BindingFlags.Instance | BindingFlags.Public);
      var serialize = typeof(BinSerializer).GetMethod(nameof(BinSerializer.Serialize), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(elementType);

      il.DeclareLocal(typeof(int));
      il.DeclareLocal(typeof(bool));

      var skipLabel = il.DefineLabel();
      var zeroBased = il.DefineLabel();
      var loopLabel = il.DefineLabel();
      var exitLoopLabel = il.DefineLabel();

      BSDebug.TraceStart(il, "Write " + type.Name);

      // Write type
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldtoken, type);
      il.Emit(OpCodes.Call, fetTypeFromHandle);
      il.Emit(OpCodes.Call, _streamWriters[typeof(Type)]);

      // Write refId
      il.Emit(OpCodes.Ldarg_0);         // Load stream
      il.Emit(OpCodes.Ldarg_1);         // Load obj
      il.Emit(OpCodes.Ldloca_S, 1);     // Load address of bool local
      il.Emit(OpCodes.Call, getRefId);  // Obj => RefId
      il.Emit(OpCodes.Call, _streamWriters[typeof(int)]);

      // Check case when reference type already serialized.
      // If null be returned then created flag be zero too and serialization will be skipped.
      il.Emit(OpCodes.Ldloc_1);
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
        il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) }));
        il.Emit(OpCodes.Throw);

        // Write array length
        il.Emit(OpCodes.Ldarg_0); // Load stream
        il.Emit(OpCodes.Ldarg_1); // Load array
        il.Emit(OpCodes.Ldlen);   // Load array length
        il.Emit(OpCodes.Call, _streamWriters[typeof(int)]);

        // Get last array index
        il.MarkLabel(zeroBased);
        il.Emit(OpCodes.Ldarg_1);  // Load array
        il.Emit(OpCodes.Ldlen);    // Load array length
        il.Emit(OpCodes.Ldc_I4_1); // Load one
        il.Emit(OpCodes.Sub);      // Subtract
        il.Emit(OpCodes.Stloc_0);  // Set to 0 local

        // Loop start
        il.MarkLabel(loopLabel);

        // Write element to stream
        il.Emit(OpCodes.Ldarg_0);             // Load stream
        il.Emit(OpCodes.Ldarg_1);             // Load array
        il.Emit(OpCodes.Ldloc_0);             // Load current index
        il.Emit(OpCodes.Ldelem, elementType); // Load element
        il.Emit(OpCodes.Call, serialize);

        // Loop check
        il.Emit(OpCodes.Ldloc_0);                // Load current index
        il.Emit(OpCodes.Brfalse, exitLoopLabel); // If zero then exit

        // Derement
        il.Emit(OpCodes.Ldloc_0);       // Load current index
        il.Emit(OpCodes.Ldc_I4_1);      // Load 1
        il.Emit(OpCodes.Sub);           // Subtract
        il.Emit(OpCodes.Stloc_0);       // Set current index
        il.Emit(OpCodes.Br, loopLabel); // Jump to loop start
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

      if (type.IsValueType)
        dynamicMethod = new DynamicMethod(string.Format("{0}_reader", type), type, new Type[] { typeof(Stream) }, type, true);
      if (type.IsClass && !type.IsArray)
        dynamicMethod = new DynamicMethod(string.Format("{0}_reader", type), typeof(object), new Type[] { typeof(Stream) }, type, true);
      if (type.IsArray)
        dynamicMethod = new DynamicMethod(string.Format("{0}_reader", type), typeof(object), new Type[] { typeof(Stream) }, type.GetElementType(), true);

      if (dynamicMethod == null)
        throw new NotImplementedException($"DynamicMethod not bilded for type: { type }");

      var il = dynamicMethod.GetILGenerator();

      if (!type.IsArray)
        CreateObjectReader(il, type);
      else
        CreateArrayReader(il, type);

      return dynamicMethod;
    }

    private static void CreateObjectReader(ILGenerator il, Type type)
    {
      var getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Public | BindingFlags.Static);
      var getUninitializedObject = typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject), BindingFlags.Public | BindingFlags.Static);
      var addRef = typeof(RefReaderWatcher).GetMethod(nameof(RefReaderWatcher.AddRef), BindingFlags.Public | BindingFlags.Static);
      var dynamicObjectRead = typeof(SerializerBuilder).GetMethod(nameof(SerializerBuilder.DynamicObjectRead), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(type);

      il.DeclareLocal(typeof(int));
      il.DeclareLocal(type);

      var resultLabel = il.DefineLabel();
      var readFastLabel = il.DefineLabel();

      // Type and reference id readed before this call method

      MethodInfo reader;
      if (_streamReaders.TryGetValue(type, out reader))
      {
        il.Emit(OpCodes.Ldarg_0); // Load stream
        il.Emit(OpCodes.Call, reader);
        il.Emit(OpCodes.Stloc_1); // Set result to local
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
        if (type.IsClass)
        {
          il.Emit(OpCodes.Ldloc_0); // Load reference id
          il.Emit(OpCodes.Ldloc_1); // Load result object reference
          il.Emit(OpCodes.Call, addRef);
        }

        // Read code type version
        int version;
        if (!Types.TryGetVersion(type, out version))
          version = 0;

        // Compare versions
        il.Emit(OpCodes.Ldc_I4, version);                   // Load program type version 
        il.Emit(OpCodes.Ldarg_0);                           // Load stream
        il.Emit(OpCodes.Call, _streamReaders[typeof(int)]); // Read saved version
        il.Emit(OpCodes.Beq, readFastLabel);

        // Call dynamic read if versions not equal
        il.Emit(OpCodes.Ldarg_0);                 // Load stream
        il.Emit(OpCodes.Call, dynamicObjectRead); // Do dynamic read
        il.Emit(OpCodes.Stloc_1);                 // Set readed value to result local
        il.Emit(OpCodes.Br, resultLabel);         // Jump to return code part

        il.MarkLabel(readFastLabel);
        foreach (var field in GetFields(type))
        {
          // Skip field id
          var fieldAttribute = field.GetCustomAttribute<FieldAttribute>(false);
          if (fieldAttribute != null)
          {
            // Skip field id format (0 for id)
            il.Emit(OpCodes.Ldarg_0); // Load stream
            il.Emit(OpCodes.Call, _streamSkipers[typeof(int)]);

            // Skip field id
            il.Emit(OpCodes.Ldarg_0); // Load stream
            il.Emit(OpCodes.Call, _streamSkipers[typeof(int)]);
          }
          else
          {
            // Skip field id format (1 for field name)
            il.Emit(OpCodes.Ldarg_0); // Load stream
            il.Emit(OpCodes.Call, _streamSkipers[typeof(int)]);

            // Skip field name
            il.Emit(OpCodes.Ldarg_0); // Load stream
            il.Emit(OpCodes.Call, _streamSkipers[typeof(string)]);
          }

          var deserialize = typeof(BinSerializer)
            .GetMethod(nameof(BinSerializer.Deserialize), BindingFlags.Static | BindingFlags.Public)
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

        // Skip end (format)
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, _streamSkipers[typeof(int)]);

        // Skip end (value)
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, _streamSkipers[typeof(int)]);
      }

      // return result
      il.MarkLabel(resultLabel);
      il.Emit(OpCodes.Ldloc_1);

      BSDebug.TraceEnd(il, "Read " + type.Name);

      il.Emit(OpCodes.Ret);
    }

    private static void CreateArrayReader(ILGenerator il, Type type)
    {
      throw new NotImplementedException();
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
      var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      Array.Sort(fields, FieldInfoComparer.Default);

      foreach (var field in fields)
      {
        // Check non serialized attribute
        var nonSerializedAttribute = field.GetCustomAttribute<NonSerializedAttribute>(false);
        if (nonSerializedAttribute != null)
          continue;

        yield return field;
      }
    }

    private class FieldInfoComparer : IComparer<FieldInfo>
    {
      public static readonly FieldInfoComparer Default = new FieldInfoComparer();

      private FieldInfoComparer()
      {
      }

      public int Compare(FieldInfo lhs, FieldInfo rhs)
      {
        var lhsAttrib = lhs.GetCustomAttribute<FieldAttribute>(false);
        var rhsAttrib = rhs.GetCustomAttribute<FieldAttribute>(false);

        if (lhsAttrib == null && rhsAttrib != null)
          return 1;
        if (lhsAttrib != null && rhsAttrib == null)
          return -1;
        if (lhsAttrib != null && rhsAttrib != null)
          return lhsAttrib.Id.CompareTo(rhsAttrib.Id);

        return StringComparer.InvariantCulture.Compare(lhs.Name, rhs.Name);
      }
    }
    #endregion
  }
}
