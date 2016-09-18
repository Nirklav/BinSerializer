using System;

namespace ThirtyNineEighty.BinSerializer
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
  public class TypeAttribute : Attribute
  {
    public int Id { get; private set; }
    public int Version { get; set; }
    public int MinSupportedVersion { get; set; }

    public TypeAttribute(int id)
    {
      if (id <= 0)
        throw new ArgumentException("Ids that less or equal to zero is reserved.");

      Id = id;
    }
  }
}
