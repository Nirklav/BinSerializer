using System.IO;

namespace ThirtyNineEighty.BinSerializer
{
  public static class BinSerializer
  {
    public static void Serialize<T>(Stream stream, T obj)
    {
      BinSerializer<T>.Serialize(stream, obj);
    }

    public static T Deserialize<T>(Stream stream)
    {
      return BinSerializer<T>.Deserialize(stream);
    }
  }

  public static class BinSerializer<T>
  {
    // Simple and powerful cache
    private static readonly Writer<T> _writerInvoker = GetWriterInvoker();
    private static readonly Reader<T> _readerInvoker = GetReaderInvoker();

    #region serialization
    public static void Serialize(Stream stream, T obj)
    {
      using (var watcher = new RefWriterWatcher())
        _writerInvoker(stream, obj);
    }

    private static Writer<T> GetWriterInvoker()
    {
      if (typeof(T).IsValueType)
        return GetValueTypeWriter();
      return GetClassWriter();
    }

    private static Writer<T> GetValueTypeWriter()
    {
      // Value type writer can be cached
      return SerializerBuilder.GetWriter<T>();
    }

    private static Writer<T> GetClassWriter()
    {
      return (stream, obj) =>
      {
        var type = obj == null ? typeof(T) : obj.GetType();
        var writer = SerializerBuilder.GetWriter(type);
        writer(stream, obj);
      };
    }
    #endregion

    #region deserialization
    public static T Deserialize(Stream stream)
    {
      using (var watcher = new RefReaderWatcher())
        return _readerInvoker(stream);
    }

    private static Reader<T> GetReaderInvoker()
    {
      if (typeof(T).IsValueType)
        return GetValueTypeReader();
      return GetClassReader();
    }

    private static Reader<T> GetValueTypeReader()
    {
      // Value type writer can be cached
      return SerializerBuilder.GetReader<T>();
    }

    private static Reader<T> GetClassReader()
    {
      return stream =>
      {
        BSDebug.TraceStart("Start read of ...");

        var type = stream.ReadType();

        BSDebug.TraceStart("... " + type.Name);

        var refId = stream.ReadInt32();
        if (refId == 0)
          return default(T);

        object deserialized;
        if (RefReaderWatcher.TryGetRef(refId, out deserialized))
          return (T)deserialized;

        var reader = SerializerBuilder.GetReader(type);
        return (T)reader(stream);
      };
    }
    #endregion
  }
}
