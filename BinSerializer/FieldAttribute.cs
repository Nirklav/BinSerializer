using System;

namespace ThirtyNineEighty.BinarySerializer
{
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public class FieldAttribute : Attribute
  {
    public string Id { get; private set; }
    
    public FieldAttribute(string id)
    {
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException("Id must have value.");

      Id = id;
    }
  }
}
