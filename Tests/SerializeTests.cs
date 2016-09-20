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

      [Field("c")]
      public int[] ArrayField;

      [Field("g")]
      public Test NullField;

      [Field("n")]
      public IJob InterfaceField;
    }

    [Type("TestStruct")]
    struct TestStruct
    {
      [Field("b")]
      public int IntField;

      [Field("a")]
      public float FloatField;
    }

    interface IJob
    {
      int JobId { get; }
    }

    [Type("Job")]
    class Job : IJob
    {
      [Field("j")]
      public int _jobId = 100;
      public int JobId { get { return _jobId; } }
    }

    [TestMethod]
    public void SerializeTest()
    {
      var test = new Test();
      test.StrField = "str value";
      test.IntField = 255;
      test.InnerField = test;
      test.InnerStructField = new TestStruct();
      test.InnerStructField.IntField = 10;
      test.InnerStructField.FloatField = 0.55f;
      test.ArrayField = new int[] { 1, 3, 3, 7 };
      test.InterfaceField = new Job();

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, test);
      stream.Position = 0;
      var test2 = BinSerializer.Deserialize<Test>(stream);

      Assert.AreEqual(test.StrField, test2.StrField);
      Assert.AreEqual(test.IntField, test2.IntField);
      Assert.AreEqual(ReferenceEquals(test, test.InnerField), ReferenceEquals(test2, test2.InnerField));
      Assert.AreEqual(test.InnerStructField.IntField, test2.InnerStructField.IntField);
      Assert.AreEqual(test.InnerStructField.FloatField, test2.InnerStructField.FloatField);
      Assert.AreEqual(test.ArrayField.Length, test2.ArrayField.Length);

      for (int i = 0; i < test.ArrayField.Length; i++)
        Assert.AreEqual(test.ArrayField[i], test2.ArrayField[i]);

      Assert.AreEqual(test.NullField, test2.NullField);
      Assert.AreEqual(((Job)test.InterfaceField).JobId, ((Job)test2.InterfaceField).JobId);
    }
  }
}
