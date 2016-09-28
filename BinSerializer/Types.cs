using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;

namespace ThirtyNineEighty.BinarySerializer
{
  public static class Types
  {
    public const string NullToken = "nil";
    public const string TypeEndToken = "end";
    public const string ArrayToken = "arr";

    // Types map
    private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    private static readonly Dictionary<string, SerializerTypeInfo> _typesById = new Dictionary<string, SerializerTypeInfo>();
    private static readonly Dictionary<Type, SerializerTypeInfo> _typesByType = new Dictionary<Type, SerializerTypeInfo>();

    // Runtime cache
    private static readonly ConcurrentDictionary<string, Type> _typeIdToTypeGenericCache = new ConcurrentDictionary<string, Type>();
    private static readonly ConcurrentDictionary<Type, string> _typeToTypeIdGenericCache = new ConcurrentDictionary<Type, string>();

    private static readonly ConcurrentDictionary<string, Type> _typeIdToTypeArrayCache = new ConcurrentDictionary<string, Type>();
    private static readonly ConcurrentDictionary<Type, string> _typeToTypeIdArrayCache = new ConcurrentDictionary<Type, string>();

    #region initialization
    [SecuritySafeCritical]
    static Types()
    {
      AddType<bool>();
      AddType<byte>();
      AddType<sbyte>();
      AddType<short>();
      AddType<ushort>();
      AddType<char>();
      AddType<int>();
      AddType<uint>();
      AddType<long>();
      AddType<ulong>();
      AddType<float>();
      AddType<double>();
      AddType<decimal>();
      AddType<string>();
      AddType<DateTime>();

      AddUserDefinedTypes();
    }

    [SecurityCritical]
    private static void AddType<T>()
    {
      var type = typeof(T);
      MethodInfo reader = null;
      MethodInfo writer = null;
      MethodInfo skiper = null;

      foreach (var method in typeof(StreamExtensions).GetMethods())
      {
        var attrib = method.GetCustomAttribute<StreamExtensionAttribute>(false);
        if (attrib == null || attrib.Type != type)
          continue;

        switch (attrib.Kind)
        {
          case StreamExtensionKind.Read: reader = method; break;
          case StreamExtensionKind.Write: writer = method; break;
          case StreamExtensionKind.Skip: skiper = method; break;
        }
      }

      AddTypeImpl(type, type.Name, 0, 0, writer, reader, skiper);
    }

    [SecurityCritical]
    private static void AddUserDefinedTypes()
    {
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        foreach (var type in assembly.DefinedTypes)
        {
          var attribute = type.GetCustomAttribute<BinTypeAttribute>(false);
          if (attribute != null)
            AddTypeImpl(type, attribute.Id, attribute.Version, attribute.MinSupportedVersion, null, null, null);
        }
      }
    }

    [SecurityCritical]
    private static void AddTypeImpl(Type type, string typeId, int version, int minSupportedVersion, MethodInfo writer, MethodInfo reader, MethodInfo skiper)
    {
      var typeInfo = new SerializerTypeInfo(type, typeId, version, minSupportedVersion, writer, reader, skiper);

      if (_typesById.ContainsKey(typeInfo.TypeId))
        throw new InvalidOperationException(string.Format("TypeInfo with this id already exist {0} by type {1}.", typeInfo.TypeId, typeInfo.Type));

      if (_typesByType.ContainsKey(typeInfo.Type))
        throw new InvalidOperationException(string.Format("TypeInfo with this Type already exist {0} by id {1}.",  typeInfo.Type, typeInfo.TypeId));

      _typesById.Add(typeId, typeInfo);
      _typesByType.Add(type, typeInfo);
    }

    [SecuritySafeCritical]
    public static void AddType(Type type, string typeId, int version, int minSppportedVersion)
    {
      AddType(type, typeId, version, minSppportedVersion, null, null);
    }

    [SecuritySafeCritical]
    public static void AddType(Type type, string typeId, int version, int minSupportedVersion, MethodInfo writer, MethodInfo reader)
    {
      _locker.EnterWriteLock();
      try
      {
        AddTypeImpl(type, typeId, version, minSupportedVersion, writer, reader, null);
      }
      finally
      {
        _locker.ExitWriteLock();
      }
    }
    #endregion

    #region get type id
    [SecurityCritical]
    internal static string GetTypeId(Type type)
    {
      _locker.EnterReadLock();
      try
      {
        // Array can't be recursive
        string arrayTypeId;
        if (TryGetArrayTypeId(type, out arrayTypeId))
          return arrayTypeId;

        return GetTypeIdImpl(type);
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }

    // Must be called read under lock
    [SecurityCritical]
    private static string GetTypeIdImpl(Type type)
    {
      string genericTypeId;
      if (_typeToTypeIdGenericCache.TryGetValue(type, out genericTypeId))
        return genericTypeId;

      var info = GetTypeInfo(type);
      return BuildTypeId(info, type);
    }
    #endregion

    #region get type
    [SecuritySafeCritical]
    public static Type GetType(string typeId)
    {
      _locker.EnterReadLock();
      try
      {
        // Array can't be recursive
        Type arrayType;
        if (TryGetArrayType(typeId, out arrayType))
          return arrayType;

        return GetTypeImpl(typeId);
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }

    // Must be called read under lock
    [SecurityCritical]
    private static Type GetTypeImpl(string typeId)
    {
      Type genericType;
      if (_typeIdToTypeGenericCache.TryGetValue(typeId, out genericType))
        return genericType;

      var info = GetTypeInfo(typeId);
      return BuildType(info, typeId);
    }
    #endregion

    #region get versions
    [SecurityCritical]
    internal static int GetVersion(Type type)
    {
      _locker.EnterReadLock();
      try
      {
        var info = GetTypeInfo(type);
        return info.Version;
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }

    [SecurityCritical]
    internal static int GetMinSupported(Type type)
    {
      _locker.EnterReadLock();
      try
      {
        var info = GetTypeInfo(type);
        return info.MinSupportedVersion;
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }
    #endregion

    #region get writer/reader/skiper
    [SecurityCritical]
    internal static MethodInfo TryGetWriter(Type type)
    {
      _locker.EnterReadLock();
      try
      {
        var info = GetTypeInfo(type);
        return info.Writer;
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }

    [SecurityCritical]
    internal static MethodInfo TryGetReader(Type type)
    {
      _locker.EnterReadLock();
      try
      {
        var info = GetTypeInfo(type);
        return info.Reader;
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }

    [SecurityCritical]
    internal static MethodInfo TryGetSkiper(Type type)
    {
      _locker.EnterReadLock();
      try
      {
        var info = GetTypeInfo(type);
        return info.Skiper;
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }
    #endregion

    #region array processing helpers
    // Must be called read under lock
    [SecurityCritical]
    private static bool TryGetArrayType(string typeId, out Type type)
    {
      if (!typeId.StartsWith(ArrayToken))
      {
        type = null;
        return false;
      }

      if (_typeIdToTypeArrayCache.TryGetValue(typeId, out type))
        return true;

      var elementTypeIdStartIdx = typeId.IndexOf('[');
      var elementTypeId = typeId.Substring(elementTypeIdStartIdx + 1, typeId.Length - elementTypeIdStartIdx - 2);
      var elementType = GetTypeImpl(elementTypeId);

      type = elementType.MakeArrayType();
      _typeIdToTypeArrayCache.TryAdd(typeId, type);
      return true;
    }

    // Must be called read under lock
    [SecurityCritical]
    private static bool TryGetArrayTypeId(Type type, out string typeId)
    {
      if (!type.IsArray)
      {
        typeId = null;
        return false;
      }
      
      if (_typeToTypeIdArrayCache.TryGetValue(type, out typeId))
        return true;

      var elementType = type.GetElementType();
      var elementTypeId = GetTypeIdImpl(elementType);

      var builder = new StringBuilder();
      builder.Append(ArrayToken);
      builder.Append('[');
      builder.Append(elementTypeId);
      builder.Append(']');

      typeId = builder.ToString();
      _typeToTypeIdArrayCache.TryAdd(type, typeId);
      return true;
    }
    #endregion

    #region type info helpers
    // Must be called read under lock
    [SecurityCritical]
    private static SerializerTypeInfo GetTypeInfo(Type type)
    {
      var normalizedType = Normalize(type);
      SerializerTypeInfo info;
      if (!_typesByType.TryGetValue(normalizedType, out info))
        throw new ArgumentException(string.Format("TypeInfo not found. For type {0}", type));
      return info;
    }

    // Must be called read under lock
    [SecurityCritical]
    private static SerializerTypeInfo GetTypeInfo(string typeId)
    {
      var normalizedTypeId = Normalize(typeId);
      SerializerTypeInfo info;
      if (!_typesById.TryGetValue(normalizedTypeId, out info))
        throw new ArgumentException(string.Format("TypeInfo not found. For typeId {0}", typeId));
      return info;
    }
    #endregion

    #region build type/typeid herlpers
    // Must be called read under lock
    [SecurityCritical]
    private static Type BuildType(SerializerTypeInfo typeInfo, string typeId)
    {
      if (!IsGenericTypeId(typeId))
        return typeInfo.Type;

      var index = 0;
      
      var types = new Type[typeInfo.Type.GenericTypeParameters.Length];
      foreach (var genericArgumentId in EnumerateGenericTypeIds(typeId))
      {
        types[index] = GetTypeImpl(genericArgumentId);
        index++;
      }

      var resultType = typeInfo.Type.MakeGenericType(types);

      // Add to cache
      _typeIdToTypeGenericCache.TryAdd(typeId, resultType);
      return resultType;
    }

    // Must be called read under lock
    [SecurityCritical]
    private static string BuildTypeId(SerializerTypeInfo typeInfo, Type type)
    {
      if (!type.IsGenericType || type.IsGenericTypeDefinition)
        return typeInfo.TypeId;

      if (type.ContainsGenericParameters)
        throw new ArgumentException(string.Format("{0} conatins generic parameters.", type));

      var builder = new StringBuilder();
      builder.Append(typeInfo.TypeId);
      builder.Append('[');

      var argumentsCount = type.GenericTypeArguments.Length;
      for (int i = 0; i < argumentsCount; i++)
      {
        var genericTypeArgument = type.GenericTypeArguments[i];
        var typeId = GetTypeIdImpl(genericTypeArgument);

        builder.Append(typeId);

        var isLast = i == argumentsCount - 1;
        if (!isLast)
          builder.Append(",");
      }

      builder.Append("]");
      var result = builder.ToString();

      // Add to cache
      _typeToTypeIdGenericCache.TryAdd(type, result);
      return result;
    }
    #endregion

    #region normilize type/typeid helpers
    [SecurityCritical]
    private static Type Normalize(Type type)
    {
      if (type.IsEnum)
        return Enum.GetUnderlyingType(type);

      if (type.IsGenericType && !type.IsGenericTypeDefinition)
        return type.GetGenericTypeDefinition();

      return type;
    }

    [SecurityCritical]
    private static string Normalize(string typeId)
    {
      var index1 = typeId.IndexOf('[');
      if (index1 < 0)
        return typeId;

      return typeId.Substring(0, index1);
    }
    #endregion

    #region generics helpers
    [SecurityCritical]
    private static bool IsGenericTypeId(string typeId)
    {
      var index1 = typeId.IndexOf('[');
      if (index1 < 0)
        return false;

      var index2 = typeId.LastIndexOf(']');
      if (index2 < 0)
        return false;

      return index2 > index1;
    }

    [SecurityCritical]
    private static IEnumerable<string> EnumerateGenericTypeIds(string genericTypeId)
    {
      var index1 = genericTypeId.IndexOf('[');
      var index2 = genericTypeId.LastIndexOf(']');

      if (index1 < 0 || index2 < 0)
        throw new ArgumentException("Is not a generic typeId");

      var builder = new StringBuilder();
      var bracketCounter = 1;

      for (int i = index1 + 1; i <= index2; i++)
      {
        var ch = genericTypeId[i];
        
        if (ch == '[')
          bracketCounter++;
        if (ch == ']')
          bracketCounter--;

        if (bracketCounter == 0)
        {
          yield return builder.ToString();
        }
        else
        {
          if (ch != ',' || bracketCounter != 1)
            builder.Append(ch);
          else
          {
            yield return builder.ToString();
            builder.Clear();
          }
        }
      }
    }
    #endregion
  }
}
