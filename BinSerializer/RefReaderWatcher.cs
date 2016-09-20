using System;
using System.Collections.Generic;

namespace ThirtyNineEighty.BinarySerializer
{
  struct RefReaderWatcher : IDisposable
  {
    [ThreadStatic]
    private static Dictionary<int, object> _idToRef;

    [ThreadStatic]
    private static bool _rootCreated;

    private readonly bool _isRoot;

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

    public static void AddRef(int refId, object reference)
    {
      _idToRef.Add(refId, reference);
    }

    public static bool TryGetRef(int refId, out object reference)
    {
      return _idToRef.TryGetValue(refId, out reference);
    }

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
