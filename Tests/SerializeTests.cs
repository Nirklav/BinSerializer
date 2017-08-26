using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Security;
using ThirtyNineEighty.BinarySerializer;
using ThirtyNineEighty.BinarySerializer.Types;

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
    [SecurityCritical]
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
    [SecurityCritical]
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
    [SecurityCritical]
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
    [SecurityCritical]
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
    [SecurityCritical]
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
    [SecurityCritical]
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
    [SecurityCritical]
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
    [SecurityCritical]
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
    [SecurityCritical]
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
    [SecurityCritical]
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
    [SecurityCritical]
    public void EmptyTest()
    {
      var input = new EmptyTestType(10);
      var output = SerializeDeserialize(input);
    }

    [TestMethod]
    [SecurityCritical]
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
    [SecurityCritical]
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
        if (GetType() != obj.GetType())
          return false;
        var e = (EqualsEntityTestType)obj;
        return e.Id == Id;
      }

      public override int GetHashCode()
      {
        return Id;
      }
    }

    [TestMethod]
    [SecurityCritical]
    public void EqualsTest()
    {
      var input = new EqualsTestType();
      input.First = new EqualsEntityTestType();
      input.Second = new EqualsEntityTestType();

      var output = SerializeDeserialize(input);

      Assert.IsFalse(ReferenceEquals(output.First, output.Second));
    }

    [TestMethod]
    [SecurityCritical]
    public void DictionarySerializeTest()
    {
      var dict = new Dictionary<int, string>();
      dict.Add(1, "1");
      dict.Add(2, "2");

      var result = SerializeDeserialize(dict);

      Assert.AreEqual(dict[1], result[1]);
      Assert.AreEqual(dict[2], result[2]);
    }

    [TestMethod]
    [SecurityCritical]
    public void ListSerializeTest()
    {
      var list = new List<string>();
      list.Add("1");
      list.Add("2");

      var result = SerializeDeserialize(list);

      Assert.AreEqual(list[0], result[0]);
      Assert.AreEqual(list[1], result[1]);
    }


    [BinType("ListRefTestType")]
    public class ListRefTestType
    {
      [BinField("o")]
      public string One;

      [BinField("Z")]
      public string Two;

      [BinField("g")]
      public int Three;
    }

    [TestMethod]
    [SecurityCritical]
    public void ListRefSerializeTest()
    {
      var list = new List<ListRefTestType>();

      list.Add(new ListRefTestType { One = "One", Two = null, Three = 3 });
      list.Add(new ListRefTestType { One = "Four", Two = null, Three = 6 });

      var result = SerializeDeserialize(list);

      Assert.AreEqual(list.Count, result.Count);

      for (int i = 0; i < list.Count; i++)
      {
        Assert.AreEqual(list[i].One, result[i].One);
        Assert.AreEqual(list[i].Two, result[i].Two);
        Assert.AreEqual(list[i].Three, result[i].Three);
      }
    }

    [TestMethod]
    [SecurityCritical]
    public void LongSerializeTest()
    {
      var l1 = 100L;
      var l2 = long.MinValue;
      var l3 = long.MaxValue;
      var l4 = long.MaxValue / 2;

      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, (object)l1);
      BinSerializer.Serialize(stream, (object)l2);
      BinSerializer.Serialize(stream, (object)l3);
      BinSerializer.Serialize(stream, (object)l4);

      stream.Position = 0;

      var tl1 = (long)BinSerializer.Deserialize<object>(stream);
      var tl2 = (long)BinSerializer.Deserialize<object>(stream);
      var tl3 = (long)BinSerializer.Deserialize<object>(stream);
      var tl4 = (long)BinSerializer.Deserialize<object>(stream);

      Assert.AreEqual(l1, tl1);
      Assert.AreEqual(l2, tl2);
      Assert.AreEqual(l3, tl3);
      Assert.AreEqual(l4, tl4);
    }

    [BinType("CallbacksTestType", Version = 42)]
    public class CallbacksTestType : IBinSerializable
    {
      [BinField("f")] public int First;
      [BinField("s")] public int Second;
      public int FromFirstAndSecond;

      [SecuritySafeCritical]
      public void OnSerializing(SerializationInfo info)
      {
        Second = First;
      }

      [SecuritySafeCritical]
      public void OnDeserialized(DeserializationInfo info)
      {
        if (info.Version == 42)
        {
          FromFirstAndSecond = First * Second;
        }
      }
    }

    [TestMethod]
    [SecurityCritical]
    public void CallbacksTest()
    {
      var instance = new CallbacksTestType();
      instance.First = 10;

      var result = SerializeDeserialize(instance);

      Assert.AreEqual(result.First, 10);
      Assert.AreEqual(result.Second, 10);
      Assert.AreEqual(result.FromFirstAndSecond, 100);
    }

    public class CallbacksTestManualType : IBinSerializable
    {
      public int First;
      public int Second;
      public int FromFirstAndSecond;

      [SecuritySafeCritical]
      public void OnSerializing(SerializationInfo info)
      {
        Second = First;
      }

      [SecuritySafeCritical]
      public void OnDeserialized(DeserializationInfo info)
      {
        if (info.Version == 42)
        {
          FromFirstAndSecond = First * Second;
        }
      }

      public static void Write(Stream stream, CallbacksTestManualType instance)
      {
        stream.Write(instance.First);
        stream.Write(instance.Second);
        stream.Write(instance.FromFirstAndSecond);
      }

      public static CallbacksTestManualType Read(Stream stream, CallbacksTestManualType instance, int version)
      {
        instance.First = stream.ReadInt32();
        instance.Second = stream.ReadInt32();
        instance.FromFirstAndSecond = stream.ReadInt32();
        return instance;
      }
    }

    [TestMethod]
    [SecurityCritical]
    public void CallbacksManualTest()
    {
      SerializerTypes.AddType(
        new BinTypeDescription(typeof(CallbacksTestManualType), "CallbacksTestManualType")
        , new BinTypeVersion(42, 0)
        , BinTypeProcess.Create<CallbacksTestManualType>(CallbacksTestManualType.Write, CallbacksTestManualType.Read)
      );

      var instance = new CallbacksTestManualType();
      instance.First = 10;

      var result = SerializeDeserialize(instance);

      Assert.AreEqual(result.First, 10);
      Assert.AreEqual(result.Second, 10);
      Assert.AreEqual(result.FromFirstAndSecond, 100);
    }

    [TestMethod]
    [SecurityCritical]
    public void TestManual()
    {
      SerializerTypes.AddType(
        new BinTypeDescription(typeof(Manual), "Manual")
        , new BinTypeVersion(10, 0)
        , BinTypeProcess.Create<Manual>(Manual.Write, Manual.Read)
      );

      var input = new Manual();
      input.FieldOne = "asd";
      input.FieldTwo = "lsd";

      var output = SerializeDeserialize(input);

      Assert.AreEqual(input.FieldOne, output.FieldOne);
      Assert.AreEqual(input.FieldTwo, output.FieldTwo);
    }

    public class Manual
    {
      public string FieldOne;
      public string FieldTwo;

      public static void Write(Stream stream, Manual instance)
      {
        stream.Write(instance.FieldOne);
        stream.Write(instance.FieldTwo);
      }

      public static Manual Read(Stream stream, Manual instance, int version)
      {
        instance.FieldOne = stream.ReadString();
        instance.FieldTwo = stream.ReadString();
        return instance;
      }
    }

    [TestMethod]
    [SecurityCritical]
    public void TestManualGeneric()
    {
      var write = typeof(SerializeTests).GetMethod("WriteManualGeneric", BindingFlags.Static | BindingFlags.Public);
      var read = typeof(SerializeTests).GetMethod("ReadManualGeneric", BindingFlags.Static | BindingFlags.Public);

      SerializerTypes.AddType(
        new BinTypeDescription(typeof(ManualGenericType<,>), "ManualGenericType")
        , new BinTypeVersion(10, 0)
        , BinTypeProcess.Create(write, read)
      );

      var input = new ManualGenericType<string, string>();
      input.FieldOne = "asd";
      input.FieldTwo = "lsd";

      var output = SerializeDeserialize(input);

      Assert.AreEqual(input.FieldOne, output.FieldOne);
      Assert.AreEqual(input.FieldTwo, output.FieldTwo);
    }

    public class ManualGenericType<T1, T2>
    {
      public T1 FieldOne;
      public T2 FieldTwo;
    }

    public static void WriteManualGeneric<T1, T2>(Stream stream, ManualGenericType<T1, T2> instance)
    {
      BinSerializer.Serialize(stream, instance.FieldOne);
      BinSerializer.Serialize(stream, instance.FieldTwo);
    }

    public static ManualGenericType<T1, T2> ReadManualGeneric<T1, T2>(Stream stream, ManualGenericType<T1, T2> instance, int version)
    {
      instance.FieldOne = BinSerializer.Deserialize<T1>(stream);
      instance.FieldTwo = BinSerializer.Deserialize<T2>(stream);
      return instance;
    }

    private static T SerializeDeserialize<T>(T input)
    {
      var stream = new MemoryStream();
      BinSerializer.Serialize(stream, input);
      stream.Position = 0;
      return BinSerializer.Deserialize<T>(stream);
    }
  }
}
