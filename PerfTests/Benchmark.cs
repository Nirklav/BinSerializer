using BenchmarkDotNet.Attributes;
using MessagePack;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ThirtyNineEighty.BinarySerializer;

namespace PerfTests
{
  public class Benchmark
  {
    private BinaryFormatter _formatter;
    private TestData _data;

    private MemoryStream _serializedStreamBin;
    private MemoryStream _serializedStreamFormatter;
    private MemoryStream _serializedStreamMessagePack;

    [GlobalSetup]
    public void Setup()
    {
      _formatter = new BinaryFormatter();

      _data = TestDataFactory.Create();

      _serializedStreamBin = new MemoryStream();
      BinSerializer.Serialize(_serializedStreamBin, _data);

      _serializedStreamFormatter = new MemoryStream();
      _formatter.Serialize(_serializedStreamFormatter, _data);

      _serializedStreamMessagePack = new MemoryStream();
      MessagePackSerializer.Serialize(_serializedStreamMessagePack, _data);
    }

    [Benchmark]
    public MemoryStream BinSerializer_Serialize()
    {
      var output = new MemoryStream(2048);
      BinSerializer.Serialize(output, _data);
      return output;
    }

    [Benchmark]
    public MemoryStream BinaryFormatter_Serialize()
    {
      var output = new MemoryStream(2048);
      _formatter.Serialize(output, _data);
      return output;
    }

    [Benchmark]
    public MemoryStream MessagePack_Serialize()
    {
      var output = new MemoryStream(2048);
      MessagePackSerializer.Serialize(output, _data);
      return output;
    }

    [Benchmark]
    public TestData BinSerializer_Deserialize()
    {
      _serializedStreamBin.Position = 0;
      return BinSerializer.Deserialize<TestData>(_serializedStreamBin);
    }

    [Benchmark]
    public TestData BinaryFormatter_Deserialize()
    {
      _serializedStreamFormatter.Position = 0;
      return (TestData) _formatter.Deserialize(_serializedStreamFormatter);
    }

    [Benchmark]
    public TestData MessagePack_Deserialize()
    {
      _serializedStreamMessagePack.Position = 0;
      return MessagePackSerializer.Deserialize<TestData>(_serializedStreamMessagePack);
    }
  }
}
