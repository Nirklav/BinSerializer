using System.IO;
using System.Security;
using System.Security.Permissions;

using ThirtyNineEighty.BinarySerializer.Types;

namespace ThirtyNineEighty.BinarySerializer
{
    /// <summary>
    /// Binary Serializer
    /// </summary>
    [SecuritySafeCritical]
    public static class BinSerializer
    {
        /// <summary>
        /// Serialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <param name="obj"></param>
        [SecuritySafeCritical]
#if !NET50
        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
        [SecurityPermission(SecurityAction.Assert, ControlEvidence = true)]
#endif
        
        public static void Serialize<T>(Stream stream, T obj)
        {
            BinSerializer<T>.Serialize(stream, obj);
        }
        /// <summary>
        /// Deserialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        [SecuritySafeCritical]
#if !NET50
        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
        [SecurityPermission(SecurityAction.Assert, ControlEvidence = true)]
#endif
        public static T Deserialize<T>(Stream stream)
        {
            return BinSerializer<T>.Deserialize(stream);
        }
    }
    /// <summary>
    /// Binary Serializer
    /// </summary>
    /// <typeparam name="T"></typeparam>

    [SecuritySafeCritical]
    static class BinSerializer<T>
    {
        private static readonly Writer<T> CachedWriter;
        private static readonly Reader<T> CachedReader;

        [SecuritySafeCritical]
        static BinSerializer()
        {
            CachedWriter = GetWriterInvoker();
            CachedReader = GetReaderInvoker();
        }

        [SecurityCritical]
        private static Writer<T> GetWriterInvoker()
        {
            return typeof(T).IsValueType || typeof(T).IsSealed
              ? SerializerBuilder.CreateWriter<T>(typeof(T))
              : null;
        }

        [SecurityCritical]
        private static Reader<T> GetReaderInvoker()
        {
            return typeof(T).IsValueType || typeof(T).IsSealed
              ? SerializerBuilder.CreateReader<T>(typeof(T))
              : null;
        }

        [SecuritySafeCritical]
        public static void Serialize(Stream stream, T obj)
        {
            using (new RefWriterWatcher(true))
            {
                if (CachedWriter != null)
                    CachedWriter(stream, obj);
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

                if (CachedReader != null)
                    return CachedReader(stream);

                var type = SerializerTypes.GetType(typeId);
                var reader = SerializerBuilder.CreateReader<T>(type);
                return reader(stream);
            }
        }
    }
}
