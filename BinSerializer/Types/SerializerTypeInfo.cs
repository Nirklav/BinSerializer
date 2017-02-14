using System;
using System.Reflection;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  class SerializerTypeInfo
  {
    protected readonly TypeInfo Type;
    protected readonly string TypeId;

    public readonly int Version;
    public readonly int MinSupportedVersion;

    private readonly MethodInfo _writer;
    private readonly MethodInfo _reader;
    private readonly MethodInfo _skiper;

    [SecurityCritical]
    public SerializerTypeInfo(BinTypeDescription description, BinTypeVersion version, BinTypeProcess process)
    {
      // Check
      if (description == null)
        throw new ArgumentNullException("description");
      if (version == null)
        throw new ArgumentNullException("version");

      // Set
      Type = description.Type.GetTypeInfo();
      TypeId = description.TypeId;

      Version = version.Version;
      MinSupportedVersion = version.MinSipportedVersion;

      if (process != null)
      {
        _writer = process.Writer;
        _reader = process.Reader;
        _skiper = process.Skiper;
      }
    }

    public virtual MethodInfo GetWriter(Type notNormalizedType)
    {
      return _writer;
    }

    public virtual MethodInfo GetReader(Type notNormalizedType)
    {
      return _reader;
    }

    public virtual MethodInfo GetSkiper(Type notNormalizedType)
    {
      return _skiper;
    }

    // Must be called under SerializerTypes read lock
    [SecuritySafeCritical]
    public virtual Type GetType(string notNormalizedTypeId)
    {
      return Type;
    }

    // Must be called under SerializerTypes read lock
    [SecuritySafeCritical]
    public virtual string GetTypeId(Type notNormalizedType)
    {
      return TypeId;
    }
  }
}
