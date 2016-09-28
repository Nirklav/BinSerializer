using System;

namespace ThirtyNineEighty.BinarySerializer
{
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public class BinFieldAttribute : Attribute
  {
    public string Id { get; private set; }
    
    public BinFieldAttribute(string id)
    {
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException("Id must have value.");

      Id = id;
    }
  }
}
