using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
  internal static class MethodAdapter
  {
    private struct TypePair : IEquatable<TypePair>
    {
      private readonly Type _from;
      private readonly Type _to;

      public TypePair(Type from, Type to)
      {
        _from = from;
        _to = to;
      }

      public override bool Equals(object obj)
      {
        if (ReferenceEquals(obj, null))
          return false;
        if (obj.GetType() != GetType())
          return false;
        return Equals((TypePair)obj);
      }

      public override int GetHashCode()
      {
        return (_from.GetHashCode() * 397) ^ _to.GetHashCode();
      }

      public bool Equals(TypePair other)
      {
        return _from == other._from && _to == other._to;
      }
    }

    private static readonly ConcurrentDictionary<TypePair, Delegate> CachedWriters = new ConcurrentDictionary<TypePair, Delegate>();
    private static readonly ConcurrentDictionary<TypePair, Delegate> CachedReaders = new ConcurrentDictionary<TypePair, Delegate>(); 

    [SecurityCritical]
    public static Writer<TTo> CastWriter<TTo>(Delegate writer, Type from)
    {
      // Check if equals
      var to = typeof(TTo);
      if (to == from)
        return (Writer<TTo>)writer;

      // Value types not supported covariance/contravariance
      if (!from.IsValueType && from.IsAssignableFrom(to))
        return (Writer<TTo>)writer;

      // Check cache
      var key = new TypePair(from, to);
      Delegate castedWriter;
      if (CachedWriters.TryGetValue(key, out castedWriter))
        return (Writer<TTo>)castedWriter;

      // Create
      var closedAdapter = typeof(WriterMethodAdapter<,>).MakeGenericType(from, to);
      var write = closedAdapter.GetMethod("Write");
      var adapter = Activator.CreateInstance(closedAdapter, writer);
      castedWriter = write.CreateDelegate(typeof(Writer<TTo>), adapter);

      // Cache
      CachedWriters.TryAdd(key, castedWriter);

      // Result
      return (Writer<TTo>)castedWriter;
    }

    [SecurityCritical]
    public static Reader<TTo> CastReader<TTo>(Delegate reader, Type from)
    {
      // Check if equals
      var to = typeof(TTo);
      if (to == from)
        return (Reader<TTo>)reader;

      // Value types not supported covariance/contravariance
      if (!from.IsValueType && to.IsAssignableFrom(from))
        return (Reader<TTo>)reader;

      // Check cache
      var key = new TypePair(from, to);
      Delegate castedReader;
      if (CachedReaders.TryGetValue(key, out castedReader))
        return (Reader<TTo>)castedReader;

      // Create
      var closedAdapter = typeof(ReaderMethodAdapter<,>).MakeGenericType(from, to);
      var write = closedAdapter.GetMethod("Read");
      var adapter = Activator.CreateInstance(closedAdapter, reader);
      castedReader = write.CreateDelegate(typeof(Reader<TTo>), adapter);

      // Cache
      CachedReaders.TryAdd(key, castedReader);

      // Result
      return (Reader<TTo>)castedReader;
    }
  }

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

  internal sealed class ReaderMethodAdapter<TFrom, TTo>
  {
    private readonly Reader<TFrom> _reader;

    [SecurityCritical]
    public ReaderMethodAdapter(Delegate reader)
    {
      _reader = (Reader<TFrom>)reader;
    }

    [SecuritySafeCritical]
    public TTo Read(Stream stream)
    {
      return (TTo)(object)_reader(stream);
    }
  }
}
