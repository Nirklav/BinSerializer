using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
  struct RefWriterWatcher : IDisposable
  {
    private sealed class RefEqualityComparer : IEqualityComparer<object>
    {
      bool IEqualityComparer<object>.Equals(object x, object y)
      {
        return ReferenceEquals(x, y);
      }

      int IEqualityComparer<object>.GetHashCode(object obj)
      {
        return RuntimeHelpers.GetHashCode(obj);
      }
    }

    private sealed class Container
    {
      public readonly Dictionary<object, int> RefIds;
      public int LastRefId;
      public bool RootCreated;

      public Container()
      {
        RefIds = new Dictionary<object, int>(new RefEqualityComparer());
      }
    }

    [ThreadStatic]
    private static Container _container;

    private readonly bool _isRoot;

    [SecuritySafeCritical]
    public RefWriterWatcher(bool unsued)
    {
      var container = _container;
      if (container == null)
        _container = container = new Container();

      if (container.RootCreated)
      {
        _isRoot = false;
      }
      else
      {
        _isRoot = true;
        container.RootCreated = true;
      }
    }

    [SecuritySafeCritical]
    public static int GetRefId(object reference, out bool created)
    {
      var container = _container;
      if (reference == null)
      {
        created = false;
        return 0;
      }

      if (container.RefIds.TryGetValue(reference, out int refId))
      {
        created = false;
        return refId;
      }

      container.RefIds.Add(reference, refId = ++container.LastRefId);
      created = true;
      return refId;
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_isRoot)
      {
        var container = _container;
        container.RefIds.Clear();
        container.LastRefId = 0;
        container.RootCreated = false;
      }
    }
  }
}
