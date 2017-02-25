using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ThirtyNineEighty.BinarySerializer;

namespace PerfTests
{
  static class Program
  {
    [Serializable]
    [BinType("Test")]
    private class Test
    {
      [BinField("1")] public long Value1;
      [BinField("2")] public int Value2;
      [BinField("3")] public string Value3;
      [BinField("4")] public Inner1 Value4;
      [BinField("5")] public Inner2 Value5;
      [BinField("VeryLongName1")] public TestEnum Value6;
      [BinField("VeryLongName2")] public Test Value7;
      [BinField("VeryLongName3")] public Inner3<int, string> Value8;
      [BinField("VeryLongName4")] public int[] Value9;
      [BinField("VeryLongName5")] public List<string> Value10;
    }

    [Serializable]
    [BinType("Inner1")]
    private class Inner1
    {
      [BinField("1")] public string Value1;
      [BinField("2")] public long Value2;
      [BinField("3")] public int Value3;
      [BinField("4")] public Inner1 Value4;
      [BinField("5")] public Inner2 Value5;
    }

    [Serializable]
    [BinType("Inner2")]
    private struct Inner2
    {
      [BinField("1")] public int Value1;
      [BinField("2")] public long Value2;
    }

    [Serializable]
    [BinType("Inner3")]
    private class Inner3<TOne, TTwo>
    {
      [BinField("1")] public TOne Value1;
      [BinField("2")] public TTwo Value2;
    }

    [Serializable]
    private enum TestEnum
    {
      One,
      Two
    }

    private const int Count = 100000;

    private static void Main(string[] args)
    {
      var t = Create();
      var sw = new Stopwatch();

      for (int i = 0; i < 10; i++)
      {
        sw.Stop();
        sw.Reset();
        sw.Start();

        TestBin(ref t);
        Console.WriteLine("Bin: {0}ms", sw.ElapsedMilliseconds);
        
        sw.Stop();
        sw.Reset();
        sw.Start();
        
        TestFormatter(ref t);
        Console.WriteLine("Formatter: {0}ms", sw.ElapsedMilliseconds);
      }
    }

    private static void TestBin(ref Test test)
    {
      MemoryStream stream = new MemoryStream();
      for (int i = 0; i < Count; i++)
      {
        BinSerializer.Serialize(stream, test);
        stream.Position = 0;
        test = BinSerializer.Deserialize<Test>(stream);
      }
    }

    private static void TestFormatter(ref Test test)
    {
      MemoryStream stream = new MemoryStream();
      var formatter = new BinaryFormatter();
      for (int i = 0; i < Count; i++)
      {
        formatter.Serialize(stream, test);
        stream.Position = 0;
        test = (Test)formatter.Deserialize(stream);
      }
    }

    private static Test Create()
    {
      var inner1 = new Inner1
      {
        Value1 = "TestStringTestStringTestString2",
        Value2 = 909091231239911,
        Value3 = 234235211,
        Value4 = new Inner1
        {
          Value1 = "TestStringTestStringTestString3",
          Value2 = 14235368796,
          Value3 = 769679679,
          Value4 = null,
          Value5 = new Inner2
          {
            Value1 = 67907807,
            Value2 = 8070780789789678
          }
        },
        Value5 = new Inner2
        {
          Value1 = 124124124,
          Value2 = 233245232234
        }
      };

      var test = new Test
      {
        Value1 = long.MaxValue / 2 + long.MaxValue / 3,
        Value2 = int.MaxValue / 2 + int.MaxValue / 3,
        Value3 = "TestStringTestStringTestString1",
        Value4 = inner1,
        Value5 = new Inner2
        {
          Value1 = 456456456,
          Value2 = 456456574556
        },
        Value6 = TestEnum.One,
        Value8 = new Inner3<int, string>
        {
          Value1 = 353254653,
          Value2 = "TestStringTestStringTestString4"
        },
        Value9 = new int[]
        {
          100,
          200,
          300,
          400,
          500,
          600,
          700,
          800,
          900
        },
        Value10 = new List<string>
        {
          "Value1",
          "Value2",
          "Value3",
          "Value4",
          "Value5",
          "Value6",
          "Value7"
        }
      };
      test.Value7 = test;
      return test;
    }
  }
}
