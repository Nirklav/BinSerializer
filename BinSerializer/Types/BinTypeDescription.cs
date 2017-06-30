using System;
using System.Collections.Generic;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  public sealed class BinTypeDescription
  {
    private static readonly HashSet<string> ReservedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      SerializerTypes.NullToken,
      SerializerTypes.TypeEndToken
    };

    private static readonly Dictionary<string, Type> ReservedTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
    {
      { SerializerTypes.ArrayToken, typeof(Array) },
      { SerializerTypes.DictionaryToken, typeof(Dictionary<,>) },
      { SerializerTypes.ListToken, typeof(List<>) },
    };

    private static readonly HashSet<char> ReservedChars = new HashSet<char>
    {
      '[', ']',
      '(', ')',
      '<', '>'
    };

    public readonly TypeImpl Type;
    public readonly string TypeId;

    public BinTypeDescription(Type type, string typeId)
      : this(new TypeImpl(type), typeId)
    {
    }

    public BinTypeDescription(TypeImpl type, string typeId)
    {
      // Type validation
      if (type.TypeInfo.IsGenericType && !type.TypeInfo.IsGenericTypeDefinition)
        throw new ArgumentException("Only opened generic types can be registered.");

      // Type id validation
      if (ReservedIds.Contains(typeId))
        throw new ArgumentException(string.Format("This id reserved by serializer {0}.", typeId));

      Type reservedType;
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
