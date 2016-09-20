using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace ThirtyNineEighty.BinSerializer
{
  public static class Types
  {
    public const string NullToken = "Null";
    public const string TypeEndToken = "Type end";
    public const string ArrayToken = "Array";

    private class TypeInfo
    {
      public readonly Type Type;
      public readonly string Id;
      public readonly int Version;
      public readonly int MinSupportedVersion;
      
      public TypeInfo(Type type, string id, int version, int minSupportedVersion)
      {
        Type = type;
        Id = id;
        Version = version;
        MinSupportedVersion = minSupportedVersion;
      }
    }

    private static readonly HashSet<string> _reservedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      NullToken,
      TypeEndToken,
      ArrayToken
    };

    private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    private static readonly Dictionary<string, TypeInfo> _typesById = new Dictionary<string, TypeInfo>();
    private static readonly Dictionary<Type, TypeInfo> _typesByType = new Dictionary<Type, TypeInfo>();

    static Types()
    {
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        foreach (var type in assembly.DefinedTypes)
        {
          var attribute = type.GetCustomAttribute<TypeAttribute>(false);
          if (attribute != null)
            AddTypeImpl(type, attribute.Id, attribute.Version, attribute.MinSupportedVersion);
        }
      }
    }

    private static void AddTypeImpl(Type type, string id, int version, int minSupportedVersion)
    {
      if (_reservedIds.Contains(id))
        throw new ArgumentException("This id reserved by serializer");

      var typeInfo = new TypeInfo(type, id, version, minSupportedVersion);

      if (_typesById.ContainsKey(typeInfo.Id))
        throw new InvalidOperationException($"TypeInfo with this id already exist { typeInfo.Id } by type { typeInfo.Type }");
      if (_typesByType.ContainsKey(typeInfo.Type))
        throw new InvalidOperationException($"TypeInfo with this Type already exist { typeInfo.Type } by id { typeInfo.Id }");

      _typesById.Add(id, typeInfo);
      _typesByType.Add(type, typeInfo);
    }

    public static void AddType(Type type, string id, int version, int minSupportedVersion)
    {
      _locker.EnterWriteLock();
      try
      {
        AddTypeImpl(type, id, version, minSupportedVersion);
      }
      finally
      {
        _locker.ExitWriteLock();
      }
    }

    public static string GetTypeId(Type type)
    {
      _locker.EnterReadLock();
      try
      {
        TypeInfo info;
        if (!_typesByType.TryGetValue(type, out info))
          throw new ArgumentException("Type id not found");
        return info.Id;
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }

    public static Type GetType(string typeId)
    {
      _locker.EnterReadLock();
      try
      {
        TypeInfo info;
        if (!_typesById.TryGetValue(typeId, out info))
          throw new ArgumentException("Type not found");
        return info.Type;
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }

    public static int GetVersion(Type type)
    {
      _locker.EnterReadLock();
      try
      {
        TypeInfo info;
        if (!_typesByType.TryGetValue(type, out info))
          throw new ArgumentException("Version not found");
        return info.Version;
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }

    public static int GetMinSupported(Type type)
    {
      _locker.EnterReadLock();
      try
      {
        TypeInfo info;
        if (!_typesByType.TryGetValue(type, out info))
          throw new ArgumentException("Min version not found");
        return info.MinSupportedVersion;
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }
  }
}
