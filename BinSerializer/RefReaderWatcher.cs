using System;
using System.Collections.Generic;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
  struct RefReaderWatcher : IDisposable
  {
    [ThreadStatic]
    private static Dictionary<int, object> _idToRef;

    [ThreadStatic]
    private static bool _rootCreated;

    private readonly bool _isRoot;

    [SecuritySafeCritical]
    public RefReaderWatcher(bool unsued)
    {
      if (_idToRef == null)
        _idToRef = new Dictionary<int, object>();

      if (_rootCreated)
      {
        _isRoot = false;
      }
      else
      {
        _isRoot = true;
        _rootCreated = true;
      }
    }

    [SecuritySafeCritical]
    public static void AddRef<T>(int refId, T reference)
    {
      _idToRef.Add(refId, reference);
    }

    [SecuritySafeCritical]
    public static bool TryGetRef<T>(int refId, out T reference)
    {
      object objRef;
      var result = _idToRef.TryGetValue(refId, out objRef);
      reference = (T)objRef;
      return result;
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_isRoot)
      {
        _idToRef.Clear();
        _rootCreated = false;
      }
    }
  }
}
