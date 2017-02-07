using System;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  public sealed class BinTypeVersion
  {
    public readonly int Version;
    public readonly int MinSipportedVersion;

    public BinTypeVersion(int version, int minSupportedVesrion)
    {
      if (version < minSupportedVesrion)
        throw new ArgumentException("version < minSupportedVesrion");

      Version = version;
      MinSipportedVersion = minSupportedVesrion;
    }
  }
}
