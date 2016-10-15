using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using ThirtyNineEighty.BinarySerializer;

namespace Tests
{
  [TestClass]
  public class SerializeTests
  {
    [BinType("NullTestType")]
    class NullTestType
    {
      [BinField("a")]
      public string StrField;
      
      [BinField("b")]
      public int IntField;

      [BinField("c")]
      public NullTestType NullField;

      [BinField("d")]
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

      var output = SerializeDeserialize(input);

      Assert.AreEqual(input.StrField, output.StrField);
      Assert.AreEqual(input.IntField, output.IntField);
      Assert.AreEqual(input.NullField, output.NullField);
      Assert.AreEqual(input.DoubleField, output.DoubleField);
    }

    [BinType("FullNullTestType")]
    class FullNullTestType
    {
      [BinField("a")]
      public string StrFieldOne;

      [BinField("b")]
      public string StrFieldTwo;
    }

    [TestMethod]
    public void FullNullTest()
    {
      var input = new FullNullTestType();

      var output = SerializeDeserialize(input);

      Assert.AreEqual(output.StrFieldOne, null);
      Assert.AreEqual(output.StrFieldOne, null);
    }

    [BinType("StructContainerType")]
    class StructContainerType
    {
      [BinField("a")]
      public StructType StructField;
    }

    [BinType("StructType")]
    struct StructType
    {
      [BinField("b")]
      public int IntField;

      [BinField("a")]
      public float FloatField;
    }

    [TestMethod]
    public void UserDefinedStructTest()
    {
      var input = new StructContainerType();
      input.StructField = new StructType();
      input.StructField.IntField = 10;
      input.StructField.FloatField = 0.55f;

      var output = SerializeDeserialize(input);

      Assert.AreEqual(input.StructField.IntField, output.StructField.IntField);
      Assert.AreEqual(input.StructField.FloatField, output.StructField.FloatField);
    }

    [BinType("CycleReferenceType")]
    class CycleReferenceType
    {
      [BinField("a")]
      public CycleReferenceType Field;
    }

    [TestMethod]
    public void CycleReferenceTest()
    {
      var input = new CycleReferenceType();
      input.Field = input;

      var output = SerializeDeserialize(input);

      Assert.AreEqual(ReferenceEquals(input, input.Field), ReferenceEquals(output, output.Field));
    }

    [BinType("InterfaceType")]
    class InterfaceType
    {
      [BinField("a")]
      public IInterface Field;
    }

    interface IInterface
    {
      int Field { get; }
    }

    [BinType("InterfaceImpl")]
    class InterfaceImpl : IInterface
    {
      [BinField("a")]
      private int _field = 100;

      public int Field { get { return _field; } }
    }

    [TestMethod]
    public void InterfaceTest()
    {
      var input = new InterfaceType();
      input.Field = new InterfaceImpl();

      var output = SerializeDeserialize(input);

      Assert.AreEqual(((InterfaceImpl)input.Field).Field, ((InterfaceImpl)output.Field).Field);
    }

    [BinType("ArrayType")]
    class ArrayType
    {
      [BinField("a")]
      public int[] ArrayField;
    }

    [TestMethod]
    public void ArrayTest()
    {
      var input = new ArrayType();
      input.ArrayField = new[] { 1, 3, 3, 7 };

      var output = SerializeDeserialize(input);

      Assert.AreEqual(input.ArrayField.Length, output.ArrayField.Length);

      for (int i = 0; i < input.ArrayField.Length; i++)
        Assert.AreEqual(input.ArrayField[i], output.ArrayField[i]);
    }

    [BinType("ArrayType2")]
    class ArrayType2
    {
      [BinField("a")]
      public ArrayElementType[] ArrayField;
    }

    [BinType("ArrayElementType")]
    public class ArrayElementType
    {
      [BinField("a")]
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

      var output = SerializeDeserialize(input);

      Assert.AreEqual(input.ArrayField.Length, output.ArrayField.Length);

      for (int i = 0; i < input.ArrayField.Length; i++)
        Assert.AreEqual(input.ArrayField[i].Field, output.ArrayField[i].Field);
    }

    [BinType("GenericType")]
    class GenericType<T1, T2>
    {
      [BinField("a")]
      public T1 FieldOne;

      [BinField("b")]
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

      var output = SerializeDeserialize(input);

      Assert.AreEqual(input.FieldOne.FieldOne, output.FieldOne.FieldOne);
      Assert.AreEqual(input.FieldOne.FieldTwo, output.FieldOne.FieldTwo);
      Assert.AreEqual(input.FieldTwo, output.FieldTwo);
    }

    [BinType("SimpleTypesRefTestType")]
    class SimpleTypesRefTestType
    {
      [BinField("a")]
      public string FieldOne;

      [BinField("b")]
      public string FieldTwo;
    }

    [TestMethod]
    public void SimpleTypesRefTest()
    {
      var input = new SimpleTypesRefTestType();
      input.FieldOne = "asdasdasdasd";
      input.FieldTwo = "asdasdasdasd";

      var output = SerializeDeserialize(input);

      Assert.AreEqual(input.FieldOne, output.FieldOne);
      Assert.AreEqual(input.FieldTwo, output.FieldTwo);
    }

    [BinType("EnumTestType")]
    class EnumTestType
    {
      [BinField("a")]
      public EnumType Field;
    }

    enum EnumType : long
    {
      One = 1,
      Two = 2,
      Three = 3
    }

    [TestMethod]
    public void EnumTest()
    {
      var input = new EnumTestType();
      input.Field = EnumType.One;

      var output = SerializeDeserialize(input);

      Assert.AreEqual(input.Field, output.Field);
    }

    [BinType("EmptyTestType")]
    class EmptyTestType
    {
      public EmptyTestType(int unused)
      {

      }
    }

    [TestMethod]
    public void EmptyTest()
    {
      var input = new EmptyTestType(10);
      var output = SerializeDeserialize(input);
    }

    [TestMethod]
    public void MultipleSerializationTest()
    {
      var input1 = new EnumTestType();
      input1.Field = EnumType.Three;

      var input2 = new GenericType<int, int>();
      input2.FieldOne = 5;
      input2.FieldTwo = 10;

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, input1);
      BinSerializer.Serialize(stream, input2);
      stream.Position = 0;

      var output1 = BinSerializer.Deserialize<EnumTestType>(stream);
      var output2 = BinSerializer.Deserialize<GenericType<int, int>>(stream);

      Assert.AreEqual(input1.Field, output1.Field);

      Assert.AreEqual(input2.FieldOne, output2.FieldOne);
      Assert.AreEqual(input2.FieldTwo, output2.FieldTwo);
    }

    [BinType("InheritorBaseType")]
    class InheritorBaseType
    {
      [BinField("0")]
      private long _zero;

      public long Zero { get { return _zero; } set { _zero = value; } }
    }

    [BinType("Inheritor2BaseType")]
    class Inheritor2BaseType : InheritorBaseType
    {
      [BinField("a")]
      private string _one;

      [BinField("b")]
      private string _two;

      [BinField("c")]
      protected string _three;

      public string Three { get { return _three; } set { _three = value; } }
      public string One { get { return _one; } set { _one = value; } }
      public string Two { get { return _two; } set { _two = value; } }
    }

    [BinType("InheritorType")]
    class InheritorType : Inheritor2BaseType
    {
      [BinField("d")]
      private string _four;

      [BinField("g")]
      private string _five;

      [BinField("h")]
      public int Six;

      public string Four { get { return _four; } set { _four = value; } }
      public string Five { get { return _five; } set { _five = value; } }
    }
    
    [TestMethod]
    public void InheritanceTest()
    {
      var input = new InheritorType();
      input.Zero = -1; // kek
      input.One = "1";
      input.Two = "2";
      input.Three = "3";
      input.Four = "4";
      input.Five = "5";
      input.Six = 6;

      var output = SerializeDeserialize(input);

      Assert.AreEqual(input.Zero, output.Zero);
      Assert.AreEqual(input.One, output.One);
      Assert.AreEqual(input.Two, output.Two);
      Assert.AreEqual(input.Three, output.Three);
      Assert.AreEqual(input.Four, output.Four);
      Assert.AreEqual(input.Five, output.Five);
      Assert.AreEqual(input.Six, output.Six);
    }

    private static T SerializeDeserialize<T>(T input)
    {
      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, input);
      stream.Position = 0;
      return BinSerializer.Deserialize<T>(stream);
    }

    [BinType("EqualsTestType")]
    public class EqualsTestType
    {
      [BinField("f")]
      public EqualsEntityTestType First;

      [BinField("s")]
      public EqualsEntityTestType Second;
    }

    [BinType("EqualsEntityTestType")]
    public class EqualsEntityTestType
    {
      [BinField("i")]
      public int Id;

      public override bool Equals(object obj)
      {
        if (ReferenceEquals(obj, null))
          return false;
        if (ReferenceEquals(obj, this))
          return true;
        var e = obj as EqualsEntityTestType;
        if (e == null)
          return false;
        return e.Id == Id;
      }

      public override int GetHashCode()
      {
        return Id;
      }
    }

    [TestMethod]
    public void EqualsTest()
    {
      var input = new EqualsTestType();
      input.First = new EqualsEntityTestType();
      input.Second = new EqualsEntityTestType();

      var output = SerializeDeserialize(input);

      Assert.IsFalse(ReferenceEquals(output.First, output.Second));
    }
  }
}
