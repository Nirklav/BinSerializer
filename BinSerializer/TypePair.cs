using System;

namespace ThirtyNineEighty.BinarySerializer
{
    internal static partial class MethodAdapter
  {
        private struct TypePair : IEquatable<TypePair>
    {
      private readonly Type _from;
      private readonly Type _to;

      public TypePair(Type from, Type to)
      {
        _from = from;
        _to = to;
      }

      public override bool Equals(object obj)
      {
        if (ReferenceEquals(obj, null))
          return false;
        if (obj.GetType() != GetType())
          return false;
        return Equals((TypePair)obj);
      }

      public override int GetHashCode()
      {
        return (_from.GetHashCode() * 397) ^ _to.GetHashCode();
      }

      public bool Equals(TypePair other)
      {
        return _from == other._from && _to == other._to;
      }
    }
  }
}
