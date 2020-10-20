using MessagePack;
using System;
using System.Collections.Generic;
using ThirtyNineEighty.BinarySerializer;

namespace PerfTests
{
  [Serializable]
  [BinType("Test")]
  [MessagePackObject]
  public class TestData
  {
    [Key(0)] [BinField("1")] public long Value1;
    [Key(1)] [BinField("2")] public int Value2;
    [Key(2)] [BinField("3")] public string Value3;
    [Key(3)] [BinField("4")] public Inner1 Value4;
    [Key(4)] [BinField("5")] public Inner2 Value5;
    [Key(5)] [BinField("VeryLongName1")] public TestEnum Value6;
    [Key(6)] [BinField("VeryLongName2")] public Inner3<int, string> Value8;
    [Key(7)] [BinField("VeryLongName3")] public int[] Value9;
    [Key(8)] [BinField("VeryLongName4")] public List<string> Value10;
  }

  [Serializable]
  public enum TestEnum
  {
    One,
    Two
  }

  [Serializable]
  [BinType("Inner1")]
  [MessagePackObject]
  public class Inner1
  {
    [Key(0)] [BinField("1")] public string Value1;
    [Key(1)] [BinField("2")] public long Value2;
    [Key(2)] [BinField("3")] public int Value3;
    [Key(3)] [BinField("4")] public Inner2 Value5;
  }

  [Serializable]
  [BinType("Inner2")]
  [MessagePackObject]
  public struct Inner2
  {
    [Key(0)] [BinField("1")] public int Value1;
    [Key(1)] [BinField("2")] public long Value2;
  }

  [Serializable]
  [BinType("Inner3")]
  [MessagePackObject]
  public class Inner3<TOne, TTwo>
  {
    [Key(0)] [BinField("1")] public TOne Value1;
    [Key(1)] [BinField("2")] public TTwo Value2;
  }
}
