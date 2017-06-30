﻿using System;
using System.Reflection;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  class SerializerTypeInfo
  {
    protected readonly TypeImpl Type;
    protected readonly string TypeId;

    public readonly int Version;
    public readonly int MinSupportedVersion;

    public readonly MethodInfo StreamWriter;
    public readonly MethodInfo StreamReader;
    public readonly MethodInfo StreamSkiper;

    private readonly MethodInfo _typeWriter;
    private readonly MethodInfo _typeReader;

    [SecurityCritical]
    public SerializerTypeInfo(BinTypeDescription description, BinTypeVersion version, BinTypeProcess process)
    {
      // Check
      if (description == null)
        throw new ArgumentNullException("description");
      if (version == null)
        throw new ArgumentNullException("version");

      // Set
      Type = description.Type;
      TypeId = description.TypeId;

      Version = version.Version;
      MinSupportedVersion = version.MinSipportedVersion;

      if (process != null)
      {
        StreamWriter = process.StreamWriter;
        StreamReader = process.StreamReader;
        StreamSkiper = process.StreamSkiper;

        _typeWriter = process.TypeWriter;
        _typeReader = process.TypeReader;
      }
    }

    public virtual MethodInfo GetTypeWriter(TypeImpl notNormalizedType)
    {
      return _typeWriter;
    }

    public virtual MethodInfo GetTypeReader(TypeImpl notNormalizedType)
    {
      return _typeReader;
    }

    // Must be called under SerializerTypes read lock
    [SecuritySafeCritical]
    public virtual TypeImpl GetType(string notNormalizedTypeId)
    {
      return Type;
    }

    // Must be called under SerializerTypes read lock
    [SecuritySafeCritical]
    public virtual string GetTypeId(TypeImpl notNormalizedType)
    {
      return TypeId;
    }
  }
}
