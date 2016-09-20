using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using ThirtyNineEighty.BinSerializer;

namespace Tests
{
  [TestClass]
  public class SerializeTests
  {
    [Type("Test")]
    class Test
    {
      [Field("a")]
      public string StrField;

      [Field("i")]
      public int IntField;
      
      [Field("b")]
      public Test InnerField;

      [Field("x")]
      public TestStruct InnerStructField;

      //[Field("c")]
      //public int[] ArrayField;
    }

    [Type("TestStruct")]
    struct TestStruct
    {
      [Field("b")]
      public int IntField;

      [Field("a")]
      public float FloatField;
    }

    [TestMethod]
    public void SerializeTest()
    {
      var testInstance = new Test();
      testInstance.StrField = "str value";
      testInstance.IntField = 255;
      testInstance.InnerField = testInstance;
      testInstance.InnerStructField = new TestStruct();
      testInstance.InnerStructField.IntField = 10;
      testInstance.InnerStructField.FloatField = 0.55f;
      //testInstance.ArrayField = new int[] { 1, 3, 3, 7 };

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, testInstance);
      stream.Position = 0;
      var deserializedTest = BinSerializer.Deserialize<object>(stream);
    }
  }
}
