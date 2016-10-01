using System.IO;
using System.Security;
using ThirtyNineEighty.BinarySerializer.Types;

namespace ThirtyNineEighty.BinarySerializer
{
  public static class BinSerializer
  {
    [SecuritySafeCritical]
    public static void Serialize<T>(Stream stream, T obj)
    {
      BinSerializer<T>.Serialize(stream, obj);
    }

    [SecuritySafeCritical]
    public static T Deserialize<T>(Stream stream)
    {
      return BinSerializer<T>.Deserialize(stream);
    }
  }

  static class BinSerializer<T>
  {
    // Simple and powerful cache
    private static readonly Writer<T> _writerInvoker;
    private static readonly Reader<T> _readerInvoker;

    [SecuritySafeCritical]
    static BinSerializer()
    {
      _writerInvoker = GetWriterInvoker();
      _readerInvoker = GetReaderInvoker();
    }

    #region serialization
    [SecuritySafeCritical]
    public static void Serialize(Stream stream, T obj)
    {
      using (var watcher = new RefWriterWatcher(true))
        _writerInvoker(stream, obj);
    }

    [SecurityCritical]
    private static Writer<T> GetWriterInvoker()
    {
      // Value type writer can be cached
      if (typeof(T).IsValueType)
        return SerializerBuilder.GetWriter<T>();
      return Write;
    }

    [SecurityCritical]
    private static void Write(Stream stream, T obj)
    {
      var type = ReferenceEquals(obj, null) ? typeof(T) : obj.GetType();
      var writer = SerializerBuilder.GetWriter(type);
      writer(stream, obj);
    }
    #endregion

    #region deserialization
    [SecuritySafeCritical]
    public static T Deserialize(Stream stream)
    {
      using (var watcher = new RefReaderWatcher(true))
        return _readerInvoker(stream);
    }

    [SecurityCritical]
    private static Reader<T> GetReaderInvoker()
    {
      if (typeof(T).IsValueType)
        return SerializerBuilder.GetReader<T>();
      return Read;
    }

    [SecurityCritical]
    private static T Read(Stream stream)
    {
      BSDebug.TraceStart("Start read of ...");

      var typeId = stream.ReadString();
      var type = SerializerTypes.GetType(typeId);

      BSDebug.TraceStart("... " + type.Name);

      var reader = SerializerBuilder.GetReader(type);
      return (T)reader(stream);
    }
    #endregion
  }
}
