using System;

namespace ThirtyNineEighty.BinSerializer
{
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public class FieldAttribute : Attribute
  {
    public int Id { get; private set; }

    public FieldAttribute(int id)
    {
      if (id <= 0)
        throw new ArgumentException("Ids that less or equal to zero is reserved.");

      Id = id;
    }
  }
}
