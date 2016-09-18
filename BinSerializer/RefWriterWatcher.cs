using System;
using System.Collections.Generic;

namespace ThirtyNineEighty.BinSerializer
{
  public struct RefWriterWatcher : IDisposable
  {
    [ThreadStatic]
    private static readonly Dictionary<object, int> _refIds = new Dictionary<object, int>();

    [ThreadStatic]
    private static int _lastRefId;

    [ThreadStatic]
    private static bool RootCreated;

    private bool isRoot;

    public RefWriterWatcher(bool unsued = true)
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

    public static int GetRefId(object reference, out bool created)
    {
      if (reference == null)
      {
        created = false;
        return 0;
      }

      int refId;
      if (_refIds.TryGetValue(reference, out refId))
      {
        created = false;
        return refId;
      }
      
      _refIds.Add(reference, refId = ++_lastRefId);
      created = true;
      return refId;
    }

    public void Dispose()
    {
      if (isRoot)
      {
        _refIds.Clear();
        RootCreated = false;
      }
    }
  }
}
