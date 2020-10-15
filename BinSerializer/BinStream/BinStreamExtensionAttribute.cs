using System;

namespace ThirtyNineEighty.BinarySerializer
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public class BinStreamExtensionAttribute : Attribute
  {
    public Type Type { get; private set; }
    public StreamExtensionKind Kind { get; private set; }

    public BinStreamExtensionAttribute(Type type, StreamExtensionKind kind)
    {
      Type = type;
      Kind = kind;
    }
  }
}
