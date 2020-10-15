using System;

namespace ThirtyNineEighty.BinarySerializer.Types
{
    /// <summary>
    /// BinTypeVersion
    /// </summary>
    public sealed class BinTypeVersion
    {
        /// <summary>
        /// Version
        /// </summary>
        public readonly int Version;
        /// <summary>
        /// MinSipportedVersion
        /// </summary>
        public readonly int MinSipportedVersion;
        /// <summary>
        /// BinTypeVersion
        /// </summary>
        /// <param name="version"></param>
        /// <param name="minSupportedVesrion"></param>
        public BinTypeVersion(int version, int minSupportedVesrion)
        {
            if (version < minSupportedVesrion)
                throw new ArgumentException("version < minSupportedVesrion");

            Version = version;
            MinSipportedVersion = minSupportedVesrion;
        }
    }
}
