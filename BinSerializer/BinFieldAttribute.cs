using System;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
    /// <summary>
    ///Binary Field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class BinFieldAttribute : Attribute
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; private set; }
        /// <summary>
        /// Binary Field
        /// </summary>
        /// <param name="id"></param>
        [SecuritySafeCritical]
        public BinFieldAttribute(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Id must have value.");

            Id = id;
        }
    }
}
