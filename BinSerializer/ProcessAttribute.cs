using System;

namespace ThirtyNineEighty.BinarySerializer
{
  enum ProcessKind
  {
    Write,
    Read,
    Skip
  }

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  class ProcessAttribute : Attribute
  {
    public string Name { get; private set; }
    public Type Type { get; private set; }
    public ProcessKind Kind { get; private set; }

    public ProcessAttribute(Type type, ProcessKind kind)
    {
      Type = type;
      Kind = kind;
    }

    public ProcessAttribute(string name, Type type, ProcessKind kind)
    {
      Name = name;
      Type = type;
      Kind = kind;
    }
  }
}