using System;
using System.IO;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
    internal sealed class ReaderMethodAdapter<TFrom, TTo>
    {
        private readonly Reader<TFrom> _reader;

        [SecurityCritical]
        public ReaderMethodAdapter(Delegate reader)
        {
            _reader = (Reader<TFrom>)reader;
        }

        [SecuritySafeCritical]
        public TTo Read(Stream stream)
        {
            return (TTo)(object)_reader(stream);
        }
    }
}
