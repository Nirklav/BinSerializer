using System;

namespace ThirtyNineEighty.BinarySerializer
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
  public class BinTypeAttribute : Attribute
  {
    public string Id { get; private set; }
    public int Version { get; set; }
    public int MinSupportedVersion { get; set; }

    public BinTypeAttribute(string id)
    {
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException("Id must have value.");

      Id = id;
    }
  }
}
