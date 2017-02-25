using System.IO;
using System.Security;
using ThirtyNineEighty.BinarySerializer.Types;

namespace ThirtyNineEighty.BinarySerializer
{
  [SecuritySafeCritical]
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

  [SecuritySafeCritical]
  static class BinSerializer<T>
  {
    private static readonly Writer<T> _cachedWriter;
    private static readonly Reader<T> _cachedReader;

    [SecuritySafeCritical]
    static BinSerializer()
    {
      _cachedWriter = GetWriterInvoker();
      _cachedReader = GetReaderInvoker();
    }

    [SecurityCritical]
    private static Writer<T> GetWriterInvoker()
    {
      return typeof(T).IsValueType
        ? SerializerBuilder.CreateWriter<T>(typeof(T))
        : null;
    }

    [SecurityCritical]
    private static Reader<T> GetReaderInvoker()
    {
      return typeof(T).IsValueType
        ? SerializerBuilder.CreateReader<T>(typeof(T))
        : null;
    }
    
    [SecuritySafeCritical]
    public static void Serialize(Stream stream, T obj)
    {
      using (new RefWriterWatcher(true))
      {
        if (_cachedWriter != null)
          _cachedWriter(stream, obj);
        else
        {
          var type = ReferenceEquals(obj, null) ? typeof(T) : obj.GetType();
          var writer = SerializerBuilder.CreateWriter<T>(type);
          writer(stream, obj);
        }
      }
    }

    [SecuritySafeCritical]
    public static T Deserialize(Stream stream)
    {
      using (new RefReaderWatcher(true))
      {
        BSDebug.TraceStart("Start read of ...");
        var typeId = stream.ReadString();   
        BSDebug.TraceStart("... " + typeId);

        if (_cachedReader != null)
          return _cachedReader(stream);
        else
        {
          var type = SerializerTypes.GetType(typeId);
          var reader = SerializerBuilder.CreateReader<T>(type);
          return reader(stream);
        }
      }
    }
  }
}
