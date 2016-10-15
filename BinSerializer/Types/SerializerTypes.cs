using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.Threading;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  public static class SerializerTypes
  {
    public const string NullToken = "nil";
    public const string TypeEndToken = "end";
    public const string ArrayToken = "arr";

    // Types map
    private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    private static readonly Dictionary<string, SerializerTypeInfo> _typesById = new Dictionary<string, SerializerTypeInfo>();
    private static readonly Dictionary<Type, SerializerTypeInfo> _typesByType = new Dictionary<Type, SerializerTypeInfo>();

    // Runtime cache
    private static readonly ConcurrentDictionary<string, Type> _typeIdToTypeCache = new ConcurrentDictionary<string, Type>();
    private static readonly ConcurrentDictionary<Type, string> _typeToTypeIdCache = new ConcurrentDictionary<Type, string>();

    #region initialization
    [SecuritySafeCritical]
    static SerializerTypes()
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

      AddArrayType();
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

      var description = new BinTypeDescription(type, type.Name);
      var version = new BinTypeVersion(0, 0);
      var process = new BinTypeProcess(writer, reader, skiper);

      AddTypeImpl(description, version, process);
    }

    [SecurityCritical]
    private static void AddArrayType()
    {
      var description = new BinTypeDescription(typeof(Array), ArrayToken);
      var version = new BinTypeVersion(0, 0);

      AddTypeImpl(description, version, null);
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
          {
            var description = new BinTypeDescription(type, attribute.Id);
            var version = new BinTypeVersion(attribute.Version, attribute.MinSupportedVersion);

            AddTypeImpl(description, version, null);
          }
        }
      }
    }

    [SecurityCritical]
    private static void AddTypeImpl(BinTypeDescription description, BinTypeVersion version, BinTypeProcess process)
    {
      if (_typesById.ContainsKey(description.TypeId))
        throw new InvalidOperationException(string.Format("TypeInfo with this id already exist {0} by type {1}.", description.TypeId, description.Type));

      if (_typesByType.ContainsKey(description.Type))
        throw new InvalidOperationException(string.Format("TypeInfo with this Type already exist {0} by id {1}.", description.Type, description.TypeId));

      if (description.Type.IsArray)
        throw new ArgumentException("Can't register array.");

      SerializerTypeInfo typeInfo;
      if (description.Type == typeof(Array))
        typeInfo = new ArraySerializerTypeInfo(description, version, process);
      else if (description.Type.IsGenericType)
        typeInfo = new GenericSerializerTypeInfo(description, version, process);
      else
        typeInfo = new SerializerTypeInfo(description, version, process);

      _typesById.Add(description.TypeId, typeInfo);
      _typesByType.Add(description.Type, typeInfo);
    }

    [SecuritySafeCritical]
    public static void AddType(BinTypeDescription description, BinTypeVersion version)
    {
      AddType(description, version, null);
    }

    [SecuritySafeCritical]
    public static void AddType(BinTypeDescription description, BinTypeVersion version, BinTypeProcess process)
    {
      _locker.EnterWriteLock();
      try
      {
        AddTypeImpl(description, version, process);
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
        return GetTypeIdImpl(type);
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }

    // Must be called read under lock
    [SecurityCritical]
    internal static string GetTypeIdImpl(Type type)
    {
      // Try return from cache
      string cachedTypeId;
      if (_typeToTypeIdCache.TryGetValue(type, out cachedTypeId))
        return cachedTypeId;

      // Resolve type id
      var info = GetTypeInfo(type);
      var typeId = info.GetTypeId(type);

      // Add to cache
      _typeToTypeIdCache.TryAdd(type, typeId);
      _typeIdToTypeCache.TryAdd(typeId, type);

      // Result
      return typeId;
    }
    #endregion

    #region get type
    [SecuritySafeCritical]
    public static Type GetType(string typeId)
    {
      _locker.EnterReadLock();
      try
      {
        return GetTypeImpl(typeId);
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }

    // Must be called read under lock
    [SecurityCritical]
    internal static Type GetTypeImpl(string typeId)
    {
      // Try return from cache
      Type cachedType;
      if (_typeIdToTypeCache.TryGetValue(typeId, out cachedType))
        return cachedType;

      // Resolve type
      var info = GetTypeInfo(typeId);
      var type = info.GetType(typeId);

      // Add to cache
      _typeToTypeIdCache.TryAdd(type, typeId);
      _typeIdToTypeCache.TryAdd(typeId, type);

      // Result
      return type;
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

    #region normalize type/typeid helpers
    [SecurityCritical]
    private static Type Normalize(Type type)
    {
      if (type.IsEnum)
        return Enum.GetUnderlyingType(type);

      if (type.IsArray)
        return typeof(Array);

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
  }
}
