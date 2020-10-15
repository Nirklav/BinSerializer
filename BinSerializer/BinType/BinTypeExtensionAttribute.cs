using System;

namespace ThirtyNineEighty.BinarySerializer
{

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  class BinTypeExtensionAttribute : Attribute
  {
    public string Name { get; private set; }
    public Type Type { get; private set; }
    public TypeExtensionKind Kind { get; private set; }

    public BinTypeExtensionAttribute(string name, Type type, TypeExtensionKind kind)
    {
      Name = name;
      Type = type;
      Kind = kind;
    }
  }
}
