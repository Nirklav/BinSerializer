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
    public const string DictionaryToken = "dct";
    public const string ListToken = "lst";

    // Types map
    private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    private static readonly Dictionary<string, SerializerTypeInfo> TypesById = new Dictionary<string, SerializerTypeInfo>();
    private static readonly Dictionary<Type, SerializerTypeInfo> TypesByType = new Dictionary<Type, SerializerTypeInfo>();

    // Runtime cache
    private static readonly ConcurrentDictionary<string, Type> TypeIdToTypeCache = new ConcurrentDictionary<string, Type>();
    private static readonly ConcurrentDictionary<Type, string> TypeToTypeIdCache = new ConcurrentDictionary<Type, string>();

    private static readonly ConcurrentDictionary<Type, MethodInfo> TypeToWritersCache = new ConcurrentDictionary<Type, MethodInfo>();
    private static readonly ConcurrentDictionary<Type, MethodInfo> TypeToReadersCache = new ConcurrentDictionary<Type, MethodInfo>();
    private static readonly ConcurrentDictionary<Type, MethodInfo> TypeToSkipersCache = new ConcurrentDictionary<Type, MethodInfo>();
 
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

      AddSystemType(typeof(Dictionary<,>));
      AddSystemType(typeof(List<>));

      AddArrayType();
      AddUserDefinedTypes();
    }

    [SecurityCritical]
    private static void AddType<T>()
    {
      AddType(typeof(T), typeof(StreamExtensions));
    }

    [SecurityCritical]
    private static void AddSystemType(Type type)
    {
      AddType(type, typeof(SystemTypesProcess));
    }

    [SecurityCritical]
    private static void AddType(Type type, Type owner)
    {
      MethodInfo reader = null;
      MethodInfo writer = null;
      MethodInfo skiper = null;
      string name = null;

      foreach (var method in owner.GetMethods())
      {
        var attrib = method.GetCustomAttribute<ProcessAttribute>(false);
        if (attrib == null || attrib.Type != type)
          continue;

        name = attrib.Name;
        switch (attrib.Kind)
        {
          case ProcessKind.Read: reader = method; break;
          case ProcessKind.Write: writer = method; break;
          case ProcessKind.Skip: skiper = method; break;
        }
      }

      var description = new BinTypeDescription(type, name ?? type.Name);
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
      if (TypesById.ContainsKey(description.TypeId))
        throw new InvalidOperationException(string.Format("TypeInfo with this id already exist {0} by type {1}.", description.TypeId, description.Type));

      if (TypesByType.ContainsKey(description.Type))
        throw new InvalidOperationException(string.Format("TypeInfo with this Type already exist {0} by id {1}.", description.Type, description.TypeId));

      if (description.Type.IsArray)
        throw new ArgumentException("Can't register array.");

      if (process != null && !process.IsValid(description.Type))
        throw new ArgumentException("Process is not valid");

      SerializerTypeInfo typeInfo;
      if (description.Type == typeof(Array))
        typeInfo = new ArraySerializerTypeInfo(description, version, process);
      else if (description.Type.IsGenericType)
        typeInfo = new GenericSerializerTypeInfo(description, version, process);
      else
        typeInfo = new SerializerTypeInfo(description, version, process);

      TypesById.Add(description.TypeId, typeInfo);
      TypesByType.Add(description.Type, typeInfo);
    }

    [SecuritySafeCritical]
    public static void AddType(BinTypeDescription description, BinTypeVersion version)
    {
      AddType(description, version, null);
    }

    [SecuritySafeCritical]
    public static void AddType(BinTypeDescription description, BinTypeVersion version, BinTypeProcess process)
    {
      Locker.EnterWriteLock();
      try
      {
        AddTypeImpl(description, version, process);
      }
      finally
      {
        Locker.ExitWriteLock();
      }
    }
    #endregion

    #region get type id
    [SecurityCritical]
    internal static string GetTypeId(Type type)
    {
      Locker.EnterReadLock();
      try
      {
        return GetTypeIdImpl(type);
      }
      finally
      {
        Locker.ExitReadLock();
      }
    }

    // Must be called under read lock
    [SecurityCritical]
    internal static string GetTypeIdImpl(Type type)
    {
      // Try return from cache
      string cachedTypeId;
      if (TypeToTypeIdCache.TryGetValue(type, out cachedTypeId))
        return cachedTypeId;

      // Resolve type id
      var info = GetTypeInfo(type);
      var typeId = info.GetTypeId(type);

      // Add to cache
      TypeToTypeIdCache.TryAdd(type, typeId);
      TypeIdToTypeCache.TryAdd(typeId, type);

      // Result
      return typeId;
    }
    #endregion

    #region get type
    [SecuritySafeCritical]
    public static Type GetType(string typeId)
    {
      Locker.EnterReadLock();
      try
      {
        return GetTypeImpl(typeId);
      }
      finally
      {
        Locker.ExitReadLock();
      }
    }

    // Must be called under read lock
    [SecurityCritical]
    internal static Type GetTypeImpl(string typeId)
    {
      // Try return from cache
      Type cachedType;
      if (TypeIdToTypeCache.TryGetValue(typeId, out cachedType))
        return cachedType;

      // Resolve type
      var info = GetTypeInfo(typeId);
      var type = info.GetType(typeId);

      // Add to cache
      TypeToTypeIdCache.TryAdd(type, typeId);
      TypeIdToTypeCache.TryAdd(typeId, type);

      // Result
      return type;
    }
    #endregion

    #region get versions
    [SecurityCritical]
    internal static int GetVersion(Type type)
    {
      Locker.EnterReadLock();
      try
      {
        var info = GetTypeInfo(type);
        return info.Version;
      }
      finally
      {
        Locker.ExitReadLock();
      }
    }

    [SecurityCritical]
    internal static int GetMinSupported(Type type)
    {
      Locker.EnterReadLock();
      try
      {
        var info = GetTypeInfo(type);
        return info.MinSupportedVersion;
      }
      finally
      {
        Locker.ExitReadLock();
      }
    }
    #endregion

    #region get writer/reader/skiper
    [SecurityCritical]
    internal static MethodInfo TryGetWriter(Type type)
    {
      Locker.EnterReadLock();
      try
      {
        // Try get from cache
        MethodInfo writer;
        if (TypeToWritersCache.TryGetValue(type, out writer))
          return writer;

        // Build
        var info = GetTypeInfo(type);
        writer = info.GetWriter(type);

        // Add to cache
        TypeToWritersCache.TryAdd(type, writer);

        // Result
        return writer;
      }
      finally
      {
        Locker.ExitReadLock();
      }
    }

    [SecurityCritical]
    internal static MethodInfo TryGetReader(Type type)
    {
      Locker.EnterReadLock();
      try
      {
        // Try get from cache
        MethodInfo reader;
        if (TypeToReadersCache.TryGetValue(type, out reader))
          return reader;

        // Build
        var info = GetTypeInfo(type);
        reader = info.GetReader(type);

        // Add to cache
        TypeToReadersCache.TryAdd(type, reader);

        // Result
        return reader;
      }
      finally
      {
        Locker.ExitReadLock();
      }
    }

    [SecurityCritical]
    internal static MethodInfo TryGetSkiper(Type type)
    {
      Locker.EnterReadLock();
      try
      {
        // Try get from cache
        MethodInfo skiper;
        if (TypeToSkipersCache.TryGetValue(type, out skiper))
          return skiper;

        // Build
        var info = GetTypeInfo(type);
        skiper = info.GetSkiper(type);

        // Add to cache
        TypeToSkipersCache.TryAdd(type, skiper);

        // Result
        return skiper;
      }
      finally
      {
        Locker.ExitReadLock();
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
      if (!TypesByType.TryGetValue(normalizedType, out info))
        throw new ArgumentException(string.Format("TypeInfo not found. For type {0}", type));
      return info;
    }

    // Must be called read under lock
    [SecurityCritical]
    private static SerializerTypeInfo GetTypeInfo(string typeId)
    {
      var normalizedTypeId = Normalize(typeId);
      SerializerTypeInfo info;
      if (!TypesById.TryGetValue(normalizedTypeId, out info))
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
