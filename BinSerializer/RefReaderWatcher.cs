using System;
using System.Collections.Generic;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
  struct RefReaderWatcher : IDisposable
  {
    private sealed class Container
    {
      public readonly Dictionary<int, object> IdToRef;
      public bool RootCreated;

      public Container()
      {
        IdToRef = new Dictionary<int, object>();
      }
    }

    [ThreadStatic]
    private static Container _container;

    private readonly bool _isRoot;

    [SecuritySafeCritical]
    public RefReaderWatcher(bool unsued)
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
    public static void AddRef<T>(int refId, T reference)
    {
      _container.IdToRef.Add(refId, reference);
    }

    [SecuritySafeCritical]
    public static bool TryGetRef<T>(int refId, out T reference)
    {
      object objRef;
      var result = _container.IdToRef.TryGetValue(refId, out objRef);
      reference = (T)objRef;
      return result;
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_isRoot)
      {
        var container = _container;
        container.IdToRef.Clear();
        container.RootCreated = false;
      }
    }
  }
}
