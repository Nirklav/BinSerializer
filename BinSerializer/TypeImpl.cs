using System;
using System.Reflection;

namespace ThirtyNineEighty.BinarySerializer
{
  public struct TypeImpl : IEquatable<TypeImpl>, IEquatable<Type>
  {
    public readonly Type Type;
    public readonly TypeInfo TypeInfo;

    public TypeImpl(Type type)
    {
      Type = type;
      TypeInfo = type.GetTypeInfo();
    }

    public TypeImpl(TypeInfo typeInfo)
    {
      Type = typeInfo.AsType();
      TypeInfo = typeInfo;
    }

    public static bool operator==(TypeImpl first, TypeImpl second)
    {
      return first.Equals(second);
    }

    public static bool operator!=(TypeImpl first, TypeImpl second)
    {
      return !first.Equals(second);
    }

    public static bool operator ==(TypeImpl first, Type second)
    {
      return first.Equals(second);
    }

    public static bool operator !=(TypeImpl first, Type second)
    {
      return !first.Equals(second);
    }

    public static bool operator ==(Type first, TypeImpl second)
    {
      return second.Equals(first);
    }

    public static bool operator !=(Type first, TypeImpl second)
    {
      return !second.Equals(first);
    }

    public bool Equals(Type other)
    {
      return Type == other;
    }

    public bool Equals(TypeImpl other)
    {
      return Type == other.Type;
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(obj, null))
        return false;
      if (!(obj is TypeImpl))
        return false;
      return Equals((TypeImpl)obj);
    }

    public override int GetHashCode()
    {
      return Type.GetHashCode();
    }

    public override string ToString()
    {
      return Type.ToString();
    }
  }
}
