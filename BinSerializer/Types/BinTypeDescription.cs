using System;
using System.Collections.Generic;
using System.Reflection;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  public sealed class BinTypeDescription
  {
    private static readonly HashSet<string> ReservedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      SerializerTypes.NullToken,
      SerializerTypes.TypeEndToken
    };

    private static readonly Dictionary<string, TypeInfo> ReservedTypes = new Dictionary<string, TypeInfo>(StringComparer.OrdinalIgnoreCase)
    {
      { SerializerTypes.ArrayToken, typeof(Array).GetTypeInfo() },
      { SerializerTypes.DictionaryToken, typeof(Dictionary<,>).GetTypeInfo() },
      { SerializerTypes.ListToken, typeof(List<>).GetTypeInfo() },
    };

    private static readonly HashSet<char> ReservedChars = new HashSet<char>
    {
      '[', ']',
      '(', ')',
      '<', '>'
    };

    public readonly TypeInfo Type;
    public readonly string TypeId;

    public BinTypeDescription(Type type, string typeId)
      : this(type.GetTypeInfo(), typeId)
    {
    }

    public BinTypeDescription(TypeInfo type, string typeId)
    {
      // Type validation
      if (type.IsGenericType && !type.IsGenericTypeDefinition)
        throw new ArgumentException("Only opened generic types can be registered.");

      // Type id validation
      if (ReservedIds.Contains(typeId))
        throw new ArgumentException(string.Format("This id reserved by serializer {0}.", typeId));

      TypeInfo reservedType;
      if (ReservedTypes.TryGetValue(typeId, out reservedType) && reservedType != type)
        throw new ArgumentException(string.Format("This id reserved by serializer {0}.", typeId));

      foreach (var ch in typeId)
        if (ReservedChars.Contains(ch))
          throw new ArgumentException("Id contains reserved symbols '[',']','(',')','<','>'.");

      // Set
      Type = type;
      TypeId = typeId;
    }
  }
}
