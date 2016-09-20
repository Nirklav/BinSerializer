using System;
using System.IO;

namespace ThirtyNineEighty.BinarySerializer
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

  static class BinSerializer<T>
  {
    // Simple and powerful cache
    private static readonly Writer<T> _writerInvoker = GetWriterInvoker();
    private static readonly Reader<T> _readerInvoker = GetReaderInvoker();

    #region serialization
    public static void Serialize(Stream stream, T obj)
    {
      using (var watcher = new RefWriterWatcher(true))
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
        var type = ReferenceEquals(obj, null) ? typeof(T) : obj.GetType();
        var writer = SerializerBuilder.GetWriter(type);
        writer(stream, obj);
      };
    }
    #endregion

    #region deserialization
    public static T Deserialize(Stream stream)
    {
      using (var watcher = new RefReaderWatcher(true))
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

        var typeId = stream.ReadString();

        Type type;
        if (!string.Equals(typeId, Types.ArrayToken, StringComparison.OrdinalIgnoreCase))
          type = Types.GetType(typeId);
        else
        {
          var arrayElementTypeId = stream.ReadString();
          var arrayElementType = Types.GetType(arrayElementTypeId);
          type = arrayElementType.MakeArrayType();
        }

        BSDebug.TraceStart("... " + type.Name);

        var reader = SerializerBuilder.GetReader(type);
        return (T)reader(stream);
      };
    }
    #endregion
  }
}
