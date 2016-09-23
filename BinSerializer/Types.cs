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

    public const string NullToken = "nil";
    public const string TypeEndToken = "end";
    public const string ArrayToken = "arr";

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

    // Types map
    private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    private static readonly Dictionary<string, SerializerTypeInfo> _typesById = new Dictionary<string, SerializerTypeInfo>();
    private static readonly Dictionary<Type, SerializerTypeInfo> _typesByType = new Dictionary<Type, SerializerTypeInfo>();

    // Runtime cache
    private static readonly ConcurrentDictionary<string, Type> _typeIdToTypeGenericCache = new ConcurrentDictionary<string, Type>();
    private static readonly ConcurrentDictionary<Type, string> _typeToTypeIdGenericCache = new ConcurrentDictionary<Type, string>();

    private static readonly ConcurrentDictionary<string, Type> _typeIdToTypeArrayCache = new ConcurrentDictionary<string, Type>();
    private static readonly ConcurrentDictionary<Type, string> _typeToTypeIdArrayCache = new ConcurrentDictionary<Type, string>();

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
        throw new ArgumentException(string.Format("This id reserved by serializer {0}.", id));

      if (type.IsGenericType && !type.IsGenericTypeDefinition)
        throw new ArgumentException("Only opened generic types can be registered.");

      foreach (var ch in id)
        if (_reservedChars.Contains(ch))
          throw new ArgumentException("Id contains reserved symbols '[',']','(',')','<','>'.");

      var typeInfo = new SerializerTypeInfo(type, id, version, minSupportedVersion);

      if (_typesById.ContainsKey(typeInfo.Id))
        throw new InvalidOperationException(string.Format("TypeInfo with this id already exist {0} by type {1}.", typeInfo.Id, typeInfo.Type));

      if (_typesByType.ContainsKey(typeInfo.Type))
        throw new InvalidOperationException(string.Format("TypeInfo with this Type already exist {0} by id {1}.",  typeInfo.Type, typeInfo.Id));

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

    // Must be called under lock
    private  static bool TryGetArrayTypeId(Type type, out string typeId)
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
        throw new ArgumentException(string.Format("{0} conatins generic parameters.", type));

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
  }
}
