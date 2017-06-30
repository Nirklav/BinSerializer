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
    private static readonly Dictionary<TypeImpl, SerializerTypeInfo> TypesByType = new Dictionary<TypeImpl, SerializerTypeInfo>();

    // Runtime cache
    private static readonly ConcurrentDictionary<string, TypeImpl> TypeIdToTypeCache = new ConcurrentDictionary<string, TypeImpl>();
    private static readonly ConcurrentDictionary<TypeImpl, string> TypeToTypeIdCache = new ConcurrentDictionary<TypeImpl, string>();

    private static readonly ConcurrentDictionary<TypeImpl, MethodInfo> TypeToTypeWritersCache = new ConcurrentDictionary<TypeImpl, MethodInfo>();
    private static readonly ConcurrentDictionary<TypeImpl, MethodInfo> TypeToTypeReadersCache = new ConcurrentDictionary<TypeImpl, MethodInfo>();
 
    #region initialization
    [SecuritySafeCritical]
    static SerializerTypes()
    {
      AddStreamType<bool>();
      AddStreamType<byte>();
      AddStreamType<sbyte>();
      AddStreamType<short>();
      AddStreamType<ushort>();
      AddStreamType<char>();
      AddStreamType<int>();
      AddStreamType<uint>();
      AddStreamType<long>();
      AddStreamType<ulong>();
      AddStreamType<float>();
      AddStreamType<double>();
      AddStreamType<decimal>();
      AddStreamType<string>();
      AddStreamType<DateTime>();

      AddType(typeof(Dictionary<,>));
      AddType(typeof(List<>));

      AddArrayType();
      AddUserDefinedTypes();
    }

    [SecurityCritical]
    private static void AddStreamType<T>()
    {
      var type = typeof(T);
      MethodInfo reader = null;
      MethodInfo writer = null;
      MethodInfo skiper = null;

      foreach (var method in typeof(BinStreamExtensions).GetTypeInfo().GetMethods())
      {
        var attrib = method.GetCustomAttribute<BinStreamExtensionAttribute>(false);
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
      var process = BinTypeProcess.CreateStreamProcess(writer, reader, skiper);

      AddTypeImpl(description, version, process);
    }

    [SecurityCritical]
    private static void AddType(Type type)
    {
      string name = null;
      MethodInfo reader = null;
      MethodInfo writer = null;

      foreach (var method in typeof(BinTypeExtensions).GetTypeInfo().GetMethods())
      {
        var attrib = method.GetCustomAttribute<BinTypeExtensionAttribute>(false);
        if (attrib == null || attrib.Type != type)
          continue;

        name = attrib.Name;
        switch (attrib.Kind)
        {
          case TypeExtensionKind.Read: reader = method; break;
          case TypeExtensionKind.Write: writer = method; break;
        }
      }

      var description = new BinTypeDescription(type, name);
      var version = new BinTypeVersion(0, 0);
      var process = BinTypeProcess.Create(writer, reader);

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
        AddTypesFrom(assembly);
    }

    [SecurityCritical]
    private static void AddTypeImpl(BinTypeDescription description, BinTypeVersion version, BinTypeProcess process)
    {
      if (TypesById.ContainsKey(description.TypeId))
        throw new InvalidOperationException(string.Format("TypeInfo with this id already exist {0} by type {1}.", description.TypeId, description.Type));

      if (TypesByType.ContainsKey(description.Type))
        throw new InvalidOperationException(string.Format("TypeInfo with this Type already exist {0} by id {1}.", description.Type, description.TypeId));

      if (description.Type.TypeInfo.IsArray)
        throw new ArgumentException("Can't register array.");

      if (process != null && !process.IsValid(description.Type))
        throw new ArgumentException("Process is not valid");

      SerializerTypeInfo typeInfo;
      if (description.Type == typeof(Array))
        typeInfo = new ArraySerializerTypeInfo(description, version, process);
      else if (description.Type.TypeInfo.IsGenericType)
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

    [SecuritySafeCritical]
    public static void AddTypesFrom(Assembly assembly)
    {
      Locker.EnterWriteLock();
      try
      {
        foreach (var type in assembly.DefinedTypes)
        {
          var attribute = type.GetCustomAttribute<BinTypeAttribute>(false);
          if (attribute != null)
          {
            var typeImpl = new TypeImpl(type);
            var description = new BinTypeDescription(typeImpl, attribute.Id);
            var version = new BinTypeVersion(attribute.Version, attribute.MinSupportedVersion);

            AddTypeImpl(description, version, null);
          }
        }
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
      return GetTypeId(new TypeImpl(type));
    }

    [SecurityCritical]
    internal static string GetTypeId(TypeImpl type)
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
      return GetTypeIdImpl(new TypeImpl(type));
    }

    // Must be called under read lock
    [SecurityCritical]
    internal static string GetTypeIdImpl(TypeImpl type)
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
    public static TypeImpl GetType(string typeId)
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
    internal static TypeImpl GetTypeImpl(string typeId)
    {
      // Try return from cache
      TypeImpl cachedType;
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
      return GetVersion(new TypeImpl(type));
    }

    [SecurityCritical]
    internal static int GetVersion(TypeImpl type)
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
      return GetMinSupported(new TypeImpl(type));
    }

    [SecurityCritical]
    internal static int GetMinSupported(TypeImpl type)
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

    #region get streamWriter/streamReader/streamSkiper
    [SecurityCritical]
    internal static MethodInfo TryGetStreamWriter(Type type)
    {
      return TryGetStreamWriter(new TypeImpl(type));
    }

    [SecurityCritical]
    internal static MethodInfo TryGetStreamWriter(TypeImpl type)
    {
      Locker.EnterReadLock();
      try
      {
        var info = GetTypeInfo(type);
        return info.StreamWriter;
      }
      finally
      {
        Locker.ExitReadLock();
      }
    }

    [SecurityCritical]
    internal static MethodInfo TryGetStreamReader(Type type)
    {
      return TryGetStreamReader(new TypeImpl(type));
    }

    [SecurityCritical]
    internal static MethodInfo TryGetStreamReader(TypeImpl type)
    {
      Locker.EnterReadLock();
      try
      {
        var info = GetTypeInfo(type);
        return info.StreamReader;
      }
      finally
      {
        Locker.ExitReadLock();
      }
    }

    [SecurityCritical]
    internal static MethodInfo TryGetStreamSkiper(Type type)
    {
      return TryGetStreamSkiper(new TypeImpl(type));
    }

    [SecurityCritical]
    internal static MethodInfo TryGetStreamSkiper(TypeImpl type)
    {
      Locker.EnterReadLock();
      try
      {
        var info = GetTypeInfo(type);
        return info.StreamSkiper;
      }
      finally
      {
        Locker.ExitReadLock();
      }
    }
    #endregion

    #region get typeWriter/typeReader
    [SecurityCritical]
    internal static MethodInfo TryGetTypeWriter(Type type)
    {
      return TryGetTypeWriter(new TypeImpl(type));
    }

    [SecurityCritical]
    internal static MethodInfo TryGetTypeWriter(TypeImpl type)
    {
      Locker.EnterReadLock();
      try
      {
        // Try get from cache
        MethodInfo writer;
        if (TypeToTypeWritersCache.TryGetValue(type, out writer))
          return writer;

        // Build
        var info = GetTypeInfo(type);
        writer = info.GetTypeWriter(type);

        // Add to cache
        TypeToTypeWritersCache.TryAdd(type, writer);

        // Result
        return writer;
      }
      finally
      {
        Locker.ExitReadLock();
      }
    }

    [SecurityCritical]
    internal static MethodInfo TryGetTypeReader(Type type)
    {
      return TryGetTypeReader(new TypeImpl(type));
    }

    [SecurityCritical]
    internal static MethodInfo TryGetTypeReader(TypeImpl type)
    {
      Locker.EnterReadLock();
      try
      {
        // Try get from cache
        MethodInfo reader;
        if (TypeToTypeReadersCache.TryGetValue(type, out reader))
          return reader;

        // Build
        var info = GetTypeInfo(type);
        reader = info.GetTypeReader(type);

        // Add to cache
        TypeToTypeReadersCache.TryAdd(type, reader);

        // Result
        return reader;
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
    private static SerializerTypeInfo GetTypeInfo(TypeImpl type)
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
    private static TypeImpl Normalize(TypeImpl type)
    {
      if (type.TypeInfo.IsEnum)
        return new TypeImpl(Enum.GetUnderlyingType(type.Type));

      if (type.TypeInfo.IsArray)
        return new TypeImpl(typeof(Array));

      if (type.TypeInfo.IsGenericType && !type.TypeInfo.IsGenericTypeDefinition)
        return new TypeImpl(type.TypeInfo.GetGenericTypeDefinition());

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
