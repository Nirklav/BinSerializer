using System;
using System.Collections.Generic;
using System.Reflection;

namespace ThirtyNineEighty.BinSerializer
{
  public static class Types
  {
    private class TypeInfo
    {
      public readonly int Id;
      public readonly int Version;
      public readonly int MinSupportedVersion;

      public TypeInfo(TypeAttribute attribute)
      {
        Id = attribute.Id;
        Version = attribute.Version;
        MinSupportedVersion = attribute.MinSupportedVersion;
      }
    }

    private static readonly Dictionary<int, Type> _idToType = new Dictionary<int, Type>();
    private static readonly Dictionary<Type, int> _typeToId = new Dictionary<Type, int>();
    private static readonly Dictionary<Type, TypeInfo> _typeInfos = new Dictionary<Type, TypeInfo>();

    static Types()
    {
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        foreach (var type in assembly.DefinedTypes)
        {
          var attribure = type.GetCustomAttribute<TypeAttribute>(false);
          if (attribure != null)
            AddType(type, attribure);
        }
      }
    }

    private static void AddType(Type type, TypeAttribute attribute)
    {
      var typeInfo = new TypeInfo(attribute);
      _idToType.Add(typeInfo.Id, type);
      _typeToId.Add(type, typeInfo.Id);
      _typeInfos.Add(type, typeInfo);
    }

    public static bool TryGetTypeId(Type type, out int typeId)
    {
      return _typeToId.TryGetValue(type, out typeId);
    }

    public static bool TryGetType(int typeId, out Type type)
    {
      return _idToType.TryGetValue(typeId, out type);
    }

    public static bool TryGetVersion(Type type, out int version)
    {
      TypeInfo info;
      if (!_typeInfos.TryGetValue(type, out info))
      {
        version = default(int);
        return false;
      }
      version = info.Version;
      return true;
    }

    public static bool TryGetMinSupported(Type type, out int minSupportedVesrsion)
    {
      TypeInfo info;
      if (!_typeInfos.TryGetValue(type, out info))
      {
        minSupportedVesrsion = default(int);
        return false;
      }
      minSupportedVesrsion = info.MinSupportedVersion;
      return true;
    }
  }
}
