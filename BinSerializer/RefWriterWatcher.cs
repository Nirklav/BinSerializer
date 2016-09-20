using System;
using System.Collections.Generic;

namespace ThirtyNineEighty.BinSerializer
{
  public struct RefWriterWatcher : IDisposable
  {
    [ThreadStatic]
    private static Dictionary<object, int> _refIds;

    [ThreadStatic]
    private static int _lastRefId;

    [ThreadStatic]
    private static bool _rootCreated;

    private readonly bool _isRoot;

    public RefWriterWatcher(bool unsued = true)
    {
      if (_refIds == null)
        _refIds = new Dictionary<object, int>();

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
      if (_isRoot)
      {
        _refIds.Clear();
        _rootCreated = false;
      }
    }
  }
}
