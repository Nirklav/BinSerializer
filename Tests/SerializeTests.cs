using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using ThirtyNineEighty.BinarySerializer;

namespace Tests
{
  [TestClass]
  public class SerializeTests
  {
    [Type("NullTestType")]
    class NullTestType
    {
      [Field("a")]
      public string StrField;
      
      [Field("b")]
      public int IntField;

      [Field("c")]
      public NullTestType NullField;

      [Field("d")]
      public double DoubleField;
    }
    
    [TestMethod]
    public void NullTest()
    {
      var input = new NullTestType();
      input.StrField = "str value";
      input.IntField = 255;
      input.NullField = null;
      input.DoubleField = 50d;

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, input);
      stream.Position = 0;
      var output = BinSerializer.Deserialize<NullTestType>(stream);

      Assert.AreEqual(input.StrField, output.StrField);
      Assert.AreEqual(input.IntField, output.IntField);
      Assert.AreEqual(input.NullField, output.NullField);
      Assert.AreEqual(input.DoubleField, output.DoubleField);
    }

    [Type("FullNullTestType")]
    class FullNullTestType
    {
      [Field("a")]
      public string StrFieldOne;

      [Field("b")]
      public string StrFieldTwo;
    }

    [TestMethod]
    public void FullNullTest()
    {
      var input = new FullNullTestType();

      using (var stream = File.Create(@"D:\file.bin"))
      {
        BinSerializer.Serialize(stream, input);
        stream.Position = 0;
        var output = BinSerializer.Deserialize<FullNullTestType>(stream);

        Assert.AreEqual(output.StrFieldOne, null);
        Assert.AreEqual(output.StrFieldOne, null);
      }
    }

    [Type("StructContainerType")]
    class StructContainerType
    {
      [Field("a")]
      public StructType StructField;
    }

    [Type("StructType")]
    struct StructType
    {
      [Field("b")]
      public int IntField;

      [Field("a")]
      public float FloatField;
    }

    [TestMethod]
    public void UserDefinedStructTest()
    {
      var input = new StructContainerType();
      input.StructField = new StructType();
      input.StructField.IntField = 10;
      input.StructField.FloatField = 0.55f;

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, input);
      stream.Position = 0;
      var output = BinSerializer.Deserialize<StructContainerType>(stream);

      Assert.AreEqual(input.StructField.IntField, output.StructField.IntField);
      Assert.AreEqual(input.StructField.FloatField, output.StructField.FloatField);
    }

    [Type("CycleReferenceType")]
    class CycleReferenceType
    {
      [Field("a")]
      public CycleReferenceType Field;
    }

    [TestMethod]
    public void CycleReferenceTest()
    {
      var input = new CycleReferenceType();
      input.Field = input;

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, input);
      stream.Position = 0;
      var output = BinSerializer.Deserialize<CycleReferenceType>(stream);

      Assert.AreEqual(ReferenceEquals(input, input.Field), ReferenceEquals(output, output.Field));
    }

    [Type("InterfaceType")]
    class InterfaceType
    {
      [Field("a")]
      public IInterface Field;
    }

    interface IInterface
    {
      int Field { get; }
    }

    [Type("InterfaceImpl")]
    class InterfaceImpl : IInterface
    {
      [Field("a")]
      private int _field = 100;

      public int Field { get { return _field; } }
    }

    [TestMethod]
    public void InterfaceTest()
    {
      var input = new InterfaceType();
      input.Field = new InterfaceImpl();

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, input);
      stream.Position = 0;
      var output = BinSerializer.Deserialize<InterfaceType>(stream);

      Assert.AreEqual(((InterfaceImpl)input.Field).Field, ((InterfaceImpl)output.Field).Field);
    }

    [Type("ArrayType")]
    class ArrayType
    {
      [Field("a")]
      public int[] ArrayField;
    }

    [TestMethod]
    public void ArrayTest()
    {
      var input = new ArrayType();
      input.ArrayField = new[] { 1, 3, 3, 7 };

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, input);
      stream.Position = 0;
      var output = BinSerializer.Deserialize<ArrayType>(stream);

      Assert.AreEqual(input.ArrayField.Length, output.ArrayField.Length);

      for (int i = 0; i < input.ArrayField.Length; i++)
        Assert.AreEqual(input.ArrayField[i], output.ArrayField[i]);
    }

    [Type("ArrayType2")]
    class ArrayType2
    {
      [Field("a")]
      public ArrayElementType[] ArrayField;
    }

    [Type("ArrayElementType")]
    public class ArrayElementType
    {
      [Field("a")]
      public int Field;

      public ArrayElementType(int f)
      {
        Field = f;
      }
    }

    [TestMethod]
    public void ArrayTest2()
    {
      var input = new ArrayType2();
      input.ArrayField = new[]
      {
        new ArrayElementType(1),
        new ArrayElementType(3),
        new ArrayElementType(3),
        new ArrayElementType(7)
      };

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, input);
      stream.Position = 0;
      var output = BinSerializer.Deserialize<ArrayType2>(stream);

      Assert.AreEqual(input.ArrayField.Length, output.ArrayField.Length);

      for (int i = 0; i < input.ArrayField.Length; i++)
        Assert.AreEqual(input.ArrayField[i].Field, output.ArrayField[i].Field);
    }

    [Type("GenericType")]
    class GenericType<T1, T2>
    {
      [Field("a")]
      public T1 FieldOne;

      [Field("b")]
      public T2 FieldTwo;
    }
    
    [TestMethod]
    public void GenericTypeTest()
    {
      var input = new GenericType<GenericType<int, int>, int>();
      input.FieldOne = new GenericType<int, int>();
      input.FieldOne.FieldOne = 500;
      input.FieldOne.FieldTwo = 300;
      input.FieldTwo = 200;

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, input);
      stream.Position = 0;
      var output = BinSerializer.Deserialize<GenericType<GenericType<int, int>, int>>(stream);

      Assert.AreEqual(input.FieldOne.FieldOne, output.FieldOne.FieldOne);
      Assert.AreEqual(input.FieldOne.FieldTwo, output.FieldOne.FieldTwo);
      Assert.AreEqual(input.FieldTwo, output.FieldTwo);
    }

    [Type("SimpleTypesRefTestType")]
    class SimpleTypesRefTestType
    {
      [Field("a")]
      public string FieldOne;

      [Field("b")]
      public string FieldTwo;
    }

    [TestMethod]
    public void SimpleTypesRefTest()
    {
      var input = new SimpleTypesRefTestType();
      input.FieldOne = "asdasdasdasd";
      input.FieldTwo = "asdasdasdasd";

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, input);
      stream.Position = 0;
      var output = BinSerializer.Deserialize<SimpleTypesRefTestType>(stream);

      Assert.AreEqual(input.FieldOne, output.FieldOne);
      Assert.AreEqual(input.FieldTwo, output.FieldTwo);
    }
  }
}
