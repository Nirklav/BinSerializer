using System;
using System.IO;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
    internal sealed class WriterMethodAdapter<TFrom, TTo>
  {
    private readonly Writer<TFrom> _writer;

    [SecurityCritical]
    public WriterMethodAdapter(Delegate writer)
    {
      _writer = (Writer<TFrom>)writer;
    }

    [SecuritySafeCritical]
    public void Write(Stream stream, TTo value)
    {
      _writer(stream, (TFrom)(object)value);
    }
  }
}
