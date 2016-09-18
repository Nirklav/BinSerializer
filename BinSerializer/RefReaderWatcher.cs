using System;
using System.Collections.Generic;

namespace ThirtyNineEighty.BinSerializer
{
  public struct RefReaderWatcher : IDisposable
  {
    [ThreadStatic]
    private static readonly Dictionary<int, object> _idToRef = new Dictionary<int, object>();

    [ThreadStatic]
    private static bool RootCreated;

    private bool isRoot;

    public RefReaderWatcher(bool unsued = true)
    {
      if (RootCreated)
      {
        isRoot = false;
      }
      else
      {
        isRoot = true;
        RootCreated = true;
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
      if (isRoot)
      {
        _idToRef.Clear();
        RootCreated = false;
      }
    }
  }
}
