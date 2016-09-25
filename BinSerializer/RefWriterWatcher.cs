using System;
using System.Collections.Generic;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
  struct RefWriterWatcher : IDisposable
  {
    [ThreadStatic]
    private static Dictionary<object, int> _refIds;

    [ThreadStatic]
    private static int _lastRefId;

    [ThreadStatic]
    private static bool _rootCreated;

    private readonly bool _isRoot;

    [SecuritySafeCritical]
    public RefWriterWatcher(bool unsued)
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

    [SecuritySafeCritical]
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

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_isRoot)
      {
        _refIds.Clear();
        _lastRefId = 0;
        _rootCreated = false;
      }
    }
  }
}
