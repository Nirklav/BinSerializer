using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
    internal static class Buffers
    {
        private const int MaxSize = 1024 * 2;

        [ThreadStatic]
        private static byte[] _buffer;

        [SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Get(int size)
        {
            if (size > MaxSize)
                return new byte[size];
            var buffer = _buffer; // Get thread static field oly once per call
            if (buffer == null)
                _buffer = buffer = new byte[MaxSize];
            return buffer;
        }
    }
}
