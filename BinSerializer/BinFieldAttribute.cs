using System;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public sealed class BinFieldAttribute : Attribute
  {
    public string Id { get; private set; }

    [SecuritySafeCritical]
    public BinFieldAttribute(string id)
    {
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException("Id must have value.");

      Id = id;
    }
  }
}
