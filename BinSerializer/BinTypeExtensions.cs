using System;
using System.Collections.Generic;
using System.IO;
using ThirtyNineEighty.BinarySerializer.Types;

namespace ThirtyNineEighty.BinarySerializer
{
  enum TypeExtensionKind
  {
    Write,
    Read
  }

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  class BinTypeExtensionAttribute : Attribute
  {
    public string Name { get; private set; }
    public Type Type { get; private set; }
    public TypeExtensionKind Kind { get; private set; }

    public BinTypeExtensionAttribute(string name, Type type, TypeExtensionKind kind)
    {
      Name = name;
      Type = type;
      Kind = kind;
    }
  }

  static class BinTypeExtensions
  {
    [BinTypeExtension(SerializerTypes.DictionaryToken, typeof(Dictionary<,>), TypeExtensionKind.Write)]
    public static void WriteDictionary<TKey, TValue>(Stream stream, Dictionary<TKey, TValue> value)
    {
      stream.Write(value.Count);
      foreach (var current in value)
      {
        BinSerializer.Serialize(stream, current.Key);
        BinSerializer.Serialize(stream, current.Value);
      }
    }

    [BinTypeExtension(SerializerTypes.DictionaryToken, typeof(Dictionary<,>), TypeExtensionKind.Read)]
    public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Stream stream, Dictionary<TKey, TValue> instance, int version)
    {
      var count = stream.ReadInt32();
      for (int i = 0; i < count; i++)
      {
        var key = BinSerializer.Deserialize<TKey>(stream);
        var value = BinSerializer.Deserialize<TValue>(stream);
        instance.Add(key, value);
      }
      return instance;
    }

    [BinTypeExtension(SerializerTypes.ListToken, typeof(List<>), TypeExtensionKind.Write)]
    public static void WriteList<T>(Stream stream, List<T> value)
    {
      stream.Write(value.Count);
      foreach (var current in value)
        BinSerializer.Serialize(stream, current);
    }

    [BinTypeExtension(SerializerTypes.ListToken, typeof(List<>), TypeExtensionKind.Read)]
    public static List<T> ReadList<T>(Stream stream, List<T> instance, int version)
    {
      var count = stream.ReadInt32();
      instance.Capacity = count;
      for (int i = 0; i < count; i++)
      {
        var value = BinSerializer.Deserialize<T>(stream);
        instance.Add(value);
      }
      return instance;
    }
  }
}
