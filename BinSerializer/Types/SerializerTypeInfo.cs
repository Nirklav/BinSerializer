using System;
using System.Reflection;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  class SerializerTypeInfo
  {
    protected readonly TypeInfo _type;
    protected readonly string _typeId;

    public readonly int Version;
    public readonly int MinSupportedVersion;

    public readonly MethodInfo Writer;
    public readonly MethodInfo Reader;
    public readonly MethodInfo Skiper;

    [SecurityCritical]
    public SerializerTypeInfo(BinTypeDescription description, BinTypeVersion version, BinTypeProcess process)
    {
      // Check
      if (description == null)
        throw new ArgumentNullException("description");
      if (version == null)
        throw new ArgumentNullException("version");

      // Set
      _type = description.Type.GetTypeInfo();
      _typeId = description.TypeId;

      Version = version.Version;
      MinSupportedVersion = version.MinSipportedVersion;

      if (process != null)
      {
        Writer = process.Writer;
        Reader = process.Reader;
        Skiper = process.Skiper;
      }
    }

    // Must be called read under SerializerTypes read lock
    [SecuritySafeCritical]
    public virtual Type GetType(string notNormalizedTypeId)
    {
      return _type;
    }

    // Must be called read under SerializerTypes read lock
    [SecuritySafeCritical]
    public virtual string GetTypeId(Type notNormalizedType)
    {
      return _typeId;
    }
  }
}
