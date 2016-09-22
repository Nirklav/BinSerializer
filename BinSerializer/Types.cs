using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ThirtyNineEighty.BinarySerializer
{
  public static class Types
  {
    public const string NullToken = "nil";
    public const string TypeEndToken = "end";
    public const string ArrayToken = "arr";

    private class SerializerTypeInfo
    {
      public readonly TypeInfo Type;
      public readonly string Id;
      public readonly int Version;
      public readonly int MinSupportedVersion;
      
      public SerializerTypeInfo(Type type, string id, int version, int minSupportedVersion)
      {
        Type = type.GetTypeInfo();
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

    private static readonly HashSet<char> _reservedChars = new HashSet<char>()
    {
      '[', ']',
      '(', ')',
      '<', '>'
    };

    private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    private static readonly Dictionary<string, SerializerTypeInfo> _typesById = new Dictionary<string, SerializerTypeInfo>();
    private static readonly Dictionary<Type, SerializerTypeInfo> _typesByType = new Dictionary<Type, SerializerTypeInfo>();

    private static readonly ConcurrentDictionary<string, Type> _typeIdToTypeGenericCache = new ConcurrentDictionary<string, Type>();
    private static readonly ConcurrentDictionary<Type, string> _typeToTypeIdGenericCache = new ConcurrentDictionary<Type, string>();

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
        throw new ArgumentException($"This id reserved by serializer { id }.");

      if (type.IsGenericType && !type.IsGenericTypeDefinition)
        throw new ArgumentException("Only opened generic types can be registered.");

      foreach (var ch in id)
        if (_reservedChars.Contains(ch))
          throw new ArgumentException("Id contains reserved symbols '[',']','(',')','<','>'.");

      var typeInfo = new SerializerTypeInfo(type, id, version, minSupportedVersion);

      if (_typesById.ContainsKey(typeInfo.Id))
        throw new InvalidOperationException($"TypeInfo with this id already exist { typeInfo.Id } by type { typeInfo.Type }.");

      if (_typesByType.ContainsKey(typeInfo.Type))
        throw new InvalidOperationException($"TypeInfo with this Type already exist { typeInfo.Type } by id { typeInfo.Id }.");

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
        return GetTypeIdImpl(type);
      }
      finally
      {
        _locker.ExitReadLock();
      }
    }

    private static string GetTypeIdImpl(Type type)
    {
      string genericTypeId;
      if (_typeToTypeIdGenericCache.TryGetValue(type, out genericTypeId))
        return genericTypeId;

      var info = GetTypeInfo(type);
      return BuildTypeId(info, type);
    }

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

    private static Type GetTypeImpl(string typeId)
    {
      Type genericType;
      if (_typeIdToTypeGenericCache.TryGetValue(typeId, out genericType))
        return genericType;

      var info = GetTypeInfo(typeId);
      return BuildType(info, typeId);
    }

    public static int GetVersion(Type type)
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

    public static int GetMinSupported(Type type)
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

    // Must be called under lock
    private static SerializerTypeInfo GetTypeInfo(Type type)
    {
      var normalizedType = Normalize(type);
      SerializerTypeInfo info;
      if (!_typesByType.TryGetValue(normalizedType, out info))
        throw new ArgumentException("TypeInfo not found");
      return info;
    }

    // Must be called under lock
    private static SerializerTypeInfo GetTypeInfo(string typeId)
    {
      var normalizedTypeId = Normalize(typeId);
      SerializerTypeInfo info;
      if (!_typesById.TryGetValue(normalizedTypeId, out info))
        throw new ArgumentException("TypeInfo not found");
      return info;
    }

    // Must be called under lock
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

    // Must be called under lock
    private static string BuildTypeId(SerializerTypeInfo typeInfo, Type type)
    {
      if (!type.IsGenericType || type.IsGenericTypeDefinition)
        return typeInfo.Id;

      if (type.ContainsGenericParameters)
        throw new ArgumentException($"{ type } conatins generic parameters.");

      var builder = new StringBuilder();
      builder.Append(typeInfo.Id);
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
    
    private static Type Normalize(Type type)
    {
      if (!type.IsGenericType || type.IsGenericTypeDefinition)
        return type;

      return type.GetGenericTypeDefinition();
    }

    private static string Normalize(string typeId)
    {
      var index1 = typeId.IndexOf('[');
      if (index1 < 0)
        return typeId;

      return typeId.Substring(0, index1);
    }

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
          if (ch != ',')
            builder.Append(ch);
          else
          {
            yield return builder.ToString();
            builder.Clear();
          }
        }
      }
    }
  }
}
