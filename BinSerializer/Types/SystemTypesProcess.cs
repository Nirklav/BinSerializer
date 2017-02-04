using System.Collections.Generic;
using System.IO;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  static class SystemTypesProcess
  {
    [BinType("Pair")]
    private class Pair<TKey, TValue>
    {
      [BinField("k")]
      public TKey Key;

      [BinField("v")]
      public TValue Value;
    }

    [Process(SerializerTypes.DictionaryToken, typeof(Dictionary<,>), ProcessKind.Write)]
    public static void WriteDictionary<TKey, TValue>(Stream stream, Dictionary<TKey, TValue> value)
    {
      stream.Write(value.Count);
      foreach (var current in value)
      {
        BinSerializer.Serialize(stream, new Pair<TKey, TValue>
        {
          Key = current.Key,
          Value = current.Value
        });
      }
    }

    [Process(SerializerTypes.DictionaryToken, typeof(Dictionary<,>), ProcessKind.Read)]
    public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Stream stream)
    {
      var count = stream.ReadInt32();
      var result = new Dictionary<TKey, TValue>(count);
      for (int i = 0; i < count; i++)
      {
        var pair = BinSerializer.Deserialize<Pair<TKey, TValue>>(stream);
        result.Add(pair.Key, pair.Value);
      }
      return result;
    }

    [Process(SerializerTypes.ListToken, typeof(List<>), ProcessKind.Write)]
    public static void WriteList<T>(Stream stream, List<T> value)
    {
      stream.Write(value.Count);
      foreach (var current in value)
        BinSerializer.Serialize(stream, current);
    }

    [Process(SerializerTypes.ListToken, typeof(List<>), ProcessKind.Read)]
    public static List<T> ReadList<T>(Stream stream)
    {
      var count = stream.ReadInt32();
      var result = new List<T>(count);
      for (int i = 0; i < count; i++)
      {
        var value = BinSerializer.Deserialize<T>(stream);
        result.Add(value);
      }
      return result;
    }
  }
}
