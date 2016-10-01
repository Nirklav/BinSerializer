using System;
using System.Collections.Generic;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  public sealed class BinTypeDescription
  {
    private static readonly HashSet<string> _reservedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      SerializerTypes.NullToken,
      SerializerTypes.TypeEndToken,
      SerializerTypes.ArrayToken
    };

    private static readonly HashSet<char> _reservedChars = new HashSet<char>()
    {
      '[', ']',
      '(', ')',
      '<', '>'
    };

    public readonly Type Type;
    public readonly string TypeId;
    
    public BinTypeDescription(Type type, string typeId)
    {
      // Type validation
      if (type.IsGenericType && !type.IsGenericTypeDefinition)
        throw new ArgumentException("Only opened generic types can be registered.");

      // Type id validation
      if (!string.Equals(typeId, SerializerTypes.ArrayToken, StringComparison.OrdinalIgnoreCase) || type != typeof(Array))
      {
        if (_reservedIds.Contains(typeId))
          throw new ArgumentException(string.Format("This id reserved by serializer {0}.", typeId));
      }

      foreach (var ch in typeId)
        if (_reservedChars.Contains(ch))
          throw new ArgumentException("Id contains reserved symbols '[',']','(',')','<','>'.");

      // Set
      Type = type;
      TypeId = typeId;
    }

    private BinTypeDescription(Type type, string typeId, bool unused)
    {
      Type = type;
      TypeId = typeId;
    }
  }
}
