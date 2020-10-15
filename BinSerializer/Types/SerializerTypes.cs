using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.Threading;

namespace ThirtyNineEighty.BinarySerializer.Types
{
    /// <summary>
    /// SerializerTypes
    /// </summary>
    public static class SerializerTypes
    {
        /// <summary>
        /// NullToken
        /// </summary>
        public const string NullToken = "nil";
        /// <summary>
        /// TypeEndToken
        /// </summary>
        public const string TypeEndToken = "end";
        /// <summary>
        /// ArrayToken
        /// </summary>
        public const string ArrayToken = "arr";
        /// <summary>
        /// DictionaryToken
        /// </summary>
        public const string DictionaryToken = "dct";
        /// <summary>
        /// ListToken
        /// </summary>
        public const string ListToken = "lst";

        // Types map
        private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static readonly Dictionary<string, SerializerTypeInfo> TypesById = new Dictionary<string, SerializerTypeInfo>();
        private static readonly Dictionary<Type, SerializerTypeInfo> TypesByType = new Dictionary<Type, SerializerTypeInfo>();

        // Runtime cache
        private static readonly ConcurrentDictionary<string, Type> TypeIdToTypeCache = new ConcurrentDictionary<string, Type>();
        private static readonly ConcurrentDictionary<Type, string> TypeToTypeIdCache = new ConcurrentDictionary<Type, string>();

        #region initialization
        [SecuritySafeCritical]
        static SerializerTypes()
        {
            AddStreamType<bool>();
            AddStreamType<byte>();
            AddStreamType<sbyte>();
            AddStreamType<short>();
            AddStreamType<ushort>();
            AddStreamType<char>();
            AddStreamType<int>();
            AddStreamType<uint>();
            AddStreamType<long>();
            AddStreamType<ulong>();
            AddStreamType<float>();
            AddStreamType<double>();
            AddStreamType<decimal>();
            AddStreamType<string>();
            AddStreamType<DateTime>();

            AddType(typeof(Dictionary<,>));
            AddType(typeof(List<>));

            AddArrayType();
            AddUserDefinedTypes();
        }

        [SecurityCritical]
        private static void AddStreamType<T>()
        {
            var type = typeof(T);
            MethodInfo reader = null;
            MethodInfo writer = null;
            MethodInfo skiper = null;

            foreach (var method in typeof(BinStreamExtensions).GetMethods())
            {
                var attrib = method.GetCustomAttribute<BinStreamExtensionAttribute>(false);
                if (attrib == null || attrib.Type != type)
                    continue;

                switch (attrib.Kind)
                {
                    case StreamExtensionKind.Read: reader = method; break;
                    case StreamExtensionKind.Write: writer = method; break;
                    case StreamExtensionKind.Skip: skiper = method; break;
                }
            }

            var description = new BinTypeDescription(type, type.Name);
            var version = new BinTypeVersion(0, 0);
            var process = BinTypeProcess.CreateStreamProcess(writer, reader, skiper);

            AddTypeImpl(description, version, process);
        }

        [SecurityCritical]
        private static void AddType(Type type)
        {
            string name = null;
            MethodInfo reader = null;
            MethodInfo writer = null;

            foreach (var method in typeof(BinTypeExtensions).GetMethods())
            {
                var attrib = method.GetCustomAttribute<BinTypeExtensionAttribute>(false);
                if (attrib == null || attrib.Type != type)
                    continue;

                name = attrib.Name;
                switch (attrib.Kind)
                {
                    case TypeExtensionKind.Read: reader = method; break;
                    case TypeExtensionKind.Write: writer = method; break;
                }
            }

            var description = new BinTypeDescription(type, name);
            var version = new BinTypeVersion(0, 0);
            var process = BinTypeProcess.Create(writer, reader);

            AddTypeImpl(description, version, process);
        }

        [SecurityCritical]
        private static void AddArrayType()
        {
            var description = new BinTypeDescription(typeof(Array), ArrayToken);
            var version = new BinTypeVersion(0, 0);

            AddTypeImpl(description, version, null);
        }

        [SecurityCritical]
        private static void AddUserDefinedTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                AddTypesFrom(assembly);
        }

        [SecurityCritical]
        private static void AddTypeImpl(BinTypeDescription description, BinTypeVersion version, BinTypeProcess process)
        {
            if (TypesById.ContainsKey(description.TypeId))
                throw new InvalidOperationException(string.Format("TypeInfo with this id already exist {0} by type {1}.", description.TypeId, description.Type));

            if (TypesByType.ContainsKey(description.Type))
                throw new InvalidOperationException(string.Format("TypeInfo with this Type already exist {0} by id {1}.", description.Type, description.TypeId));

            if (description.Type.IsArray)
                throw new ArgumentException("Can't register array.");

            if (process != null && !process.IsValid(description.Type))
                throw new ArgumentException("Process is not valid");

            SerializerTypeInfo typeInfo;
            if (description.Type == typeof(Array))
                typeInfo = new ArraySerializerTypeInfo(description, version, process);
            else if (description.Type.IsGenericType)
                typeInfo = new GenericSerializerTypeInfo(description, version, process);
            else
                typeInfo = new SerializerTypeInfo(description, version, process);

            TypesById.Add(description.TypeId, typeInfo);
            TypesByType.Add(description.Type, typeInfo);
        }

        [SecuritySafeCritical]
        public static void AddType(BinTypeDescription description, BinTypeVersion version)
        {
            AddType(description, version, null);
        }

        [SecuritySafeCritical]
        public static void AddType(BinTypeDescription description, BinTypeVersion version, BinTypeProcess process)
        {
            Locker.EnterWriteLock();
            try
            {
                AddTypeImpl(description, version, process);
            }
            finally
            {
                Locker.ExitWriteLock();
            }
        }

        [SecuritySafeCritical]
        public static void AddTypesFrom(Assembly assembly)
        {
            Locker.EnterWriteLock();
            try
            {
                foreach (var type in assembly.DefinedTypes)
                {
                    var attribute = type.GetCustomAttribute<BinTypeAttribute>(false);
                    if (attribute != null)
                    {
                        var description = new BinTypeDescription(type, attribute.Id);
                        var version = new BinTypeVersion(attribute.Version, attribute.MinSupportedVersion);

                        AddTypeImpl(description, version, null);
                    }
                }
            }
            finally
            {
                Locker.ExitWriteLock();
            }
        }
        #endregion

        #region get type id
        [SecurityCritical]
        internal static string GetTypeId(Type type)
        {
            Locker.EnterReadLock();
            try
            {
                return GetTypeIdImpl(type);
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }

        // Must be called under read lock
        [SecurityCritical]
        internal static string GetTypeIdImpl(Type type)
        {
            // Try return from cache
            string cachedTypeId;
            if (TypeToTypeIdCache.TryGetValue(type, out cachedTypeId))
                return cachedTypeId;

            // Resolve type id
            var info = GetTypeInfo(type);
            var typeId = info.GetTypeId(type);

            // Add to cache
            TypeToTypeIdCache.TryAdd(type, typeId);
            TypeIdToTypeCache.TryAdd(typeId, type);

            // Result
            return typeId;
        }
        #endregion

        #region get type
        [SecuritySafeCritical]
        public static Type GetType(string typeId)
        {
            Locker.EnterReadLock();
            try
            {
                return GetTypeImpl(typeId);
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }

        // Must be called under read lock
        [SecurityCritical]
        internal static Type GetTypeImpl(string typeId)
        {
            // Try return from cache
            Type cachedType;
            if (TypeIdToTypeCache.TryGetValue(typeId, out cachedType))
                return cachedType;

            // Resolve type
            var info = GetTypeInfo(typeId);
            var type = info.GetType(typeId);

            // Add to cache
            TypeToTypeIdCache.TryAdd(type, typeId);
            TypeIdToTypeCache.TryAdd(typeId, type);

            // Result
            return type;
        }
        #endregion

        #region get versions
        [SecurityCritical]
        internal static int GetVersion(Type type)
        {
            Locker.EnterReadLock();
            try
            {
                var info = GetTypeInfo(type);
                return info.Version;
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }

        [SecurityCritical]
        internal static int GetMinSupported(Type type)
        {
            Locker.EnterReadLock();
            try
            {
                var info = GetTypeInfo(type);
                return info.MinSupportedVersion;
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }
        #endregion

        #region get streamWriter/streamReader/streamSkiper
        [SecurityCritical]
        internal static MethodInfo TryGetStreamWriter(Type type)
        {
            Locker.EnterReadLock();
            try
            {
                var info = GetTypeInfo(type);
                return info.StreamWriter;
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }

        [SecurityCritical]
        internal static MethodInfo TryGetStreamReader(Type type)
        {
            Locker.EnterReadLock();
            try
            {
                var info = GetTypeInfo(type);
                return info.StreamReader;
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }

        [SecurityCritical]
        internal static MethodInfo TryGetStreamSkiper(Type type)
        {
            Locker.EnterReadLock();
            try
            {
                var info = GetTypeInfo(type);
                return info.StreamSkiper;
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }
        #endregion

        #region get typeWriter/typeReader
        [SecurityCritical]
        internal static MethodInfo TryGetTypeWriter(Type type)
        {
            Locker.EnterReadLock();
            try
            {
                var info = GetTypeInfo(type);
                return info.GetTypeWriter(type);
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }

        [SecurityCritical]
        internal static MethodInfo TryGetTypeReader(Type type)
        {
            Locker.EnterReadLock();
            try
            {
                var info = GetTypeInfo(type);
                return info.GetTypeReader(type);
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }
        #endregion

        #region type info helpers
        // Must be called read under lock
        [SecurityCritical]
        private static SerializerTypeInfo GetTypeInfo(Type type)
        {
            var normalizedType = Normalize(type);
            SerializerTypeInfo info;
            if (!TypesByType.TryGetValue(normalizedType, out info))
                throw new ArgumentException(string.Format("TypeInfo not found. For type {0}", type));
            return info;
        }

        // Must be called read under lock
        [SecurityCritical]
        private static SerializerTypeInfo GetTypeInfo(string typeId)
        {
            var normalizedTypeId = Normalize(typeId);
            SerializerTypeInfo info;
            if (!TypesById.TryGetValue(normalizedTypeId, out info))
                throw new ArgumentException(string.Format("TypeInfo not found. For typeId {0}", typeId));
            return info;
        }
        #endregion

        #region normalize type/typeid helpers
        [SecurityCritical]
        private static Type Normalize(Type type)
        {
            if (type.IsEnum)
                return Enum.GetUnderlyingType(type);

            if (type.IsArray)
                return typeof(Array);

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
                return type.GetGenericTypeDefinition();

            return type;
        }

        [SecurityCritical]
        private static string Normalize(string typeId)
        {
            var index1 = typeId.IndexOf('[');
            if (index1 < 0)
                return typeId;

            return typeId.Substring(0, index1);
        }
        #endregion
    }
}
