using System;
using System.Collections.Generic;
using System.Reflection;

namespace ThirtyNineEighty.BinarySerializer
{
  class SerializerTypeInfo
  {
    private static readonly HashSet<string> _reservedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      Types.NullToken,
      Types.TypeEndToken,
      Types.ArrayToken
    };

    private static readonly HashSet<char> _reservedChars = new HashSet<char>()
    {
      '[', ']',
      '(', ')',
      '<', '>'
    };

    public readonly TypeInfo Type;
    public readonly string TypeId;
    public readonly int Version;
    public readonly int MinSupportedVersion;

    public readonly MethodInfo Writer;
    public readonly MethodInfo Reader;
    public readonly MethodInfo Skiper;

    public SerializerTypeInfo(Type type, string typeId, int version, int minSupportedVersion, MethodInfo writer, MethodInfo reader, MethodInfo skiper)
    {
      // Type validation
      if (type.IsGenericType && !type.IsGenericTypeDefinition)
        throw new ArgumentException("Only opened generic types can be registered.");

      // Type id validation
      if (_reservedIds.Contains(typeId))
        throw new ArgumentException(string.Format("This id reserved by serializer {0}.", typeId));

      foreach (var ch in typeId)
        if (_reservedChars.Contains(ch))
          throw new ArgumentException("Id contains reserved symbols '[',']','(',')','<','>'.");

      // Methods validation
      if (writer != null && !writer.IsStatic)
        throw new ArgumentException("Writer method must be static.");

      if (reader != null && !reader.IsStatic)
        throw new ArgumentException("Reader method must be static.");

      if (skiper != null && !skiper.IsStatic)
        throw new ArgumentException("Skiper method must be static.");

      // Set
      Type = type.GetTypeInfo();
      TypeId = typeId;
      Version = version;
      MinSupportedVersion = minSupportedVersion;

      Writer = writer;
      Reader = reader;
      Skiper = skiper;
    }
  }
}
