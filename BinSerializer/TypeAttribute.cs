﻿using System;

namespace ThirtyNineEighty.BinSerializer
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
  public class TypeAttribute : Attribute
  {
    public string Id { get; private set; }
    public int Version { get; set; }
    public int MinSupportedVersion { get; set; }

    public TypeAttribute(string id)
    {
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException("Id must have value.");

      Id = id;
    }
  }
}