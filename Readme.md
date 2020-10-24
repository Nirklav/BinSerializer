## Binary serializer
- Inheritance support.
- Generics support.
- References supprot.
- Backward compatibility (with versions).
- ValueTypes being serialized and deserialized without boxing (Except for the case when type versions not equals).
- Four times faster than BinaryFormatter.
- Supports manual serialization and deserialzation.

### Install:
You can install library from nuget:
https://www.nuget.org/packages/ThirtyNineEighty.BinarySerializer

### Usage:
``` C#
    BinSerializer.Serialize<SomeType>(stream, input);
    stream.Position = 0;
    var output = BinSerializer.Deserialize<SomeType>(stream);
```

You can also serialize and deserialize as base type:
``` C#
    var input = new SomeType();
    BinSerializer.Serialize<object>(stream, input);
    stream.Position = 0;
    var output = (SomeType)BinSerializer.Deserialize<object>(stream);
```

### Type definition:
``` C#
    [BinType("SimpleType")]
    public class SimpleType
    {
      [BinField("a")]
      public string StrField;
      
      [BinField("b")]
      public int IntField;

      [BinField("c")]
      public SimpleType Next; // Supports cycle references

      [BinField("d")]
      public double DoubleField;
    }
```

With generics:
``` C#
    [BinType("GenericType")]
    public class GenericType<T1, T2>
    {
      [BinField("a")]
      public T1 FieldOne;

      [BinField("b")]
      public T2 FieldTwo;
    }
```

With version:

Version should be increased when fields were added or removed from type.

And with callbacks you always can process old data manually.
``` C#
    [BinType("Example", Version = 42)]
    public class Example : IBinSerializable
    {
      [BinField("f")] public int First;
      [BinField("s")] public int Second;

      public void OnSerializing(SerializationInfo info)
      {
        // ...
      }

      public void OnDeserialized(DeserializationInfo info)
      {
        if (info.Version == 42)
        {
          // ...
        }
      }
    }
```

#### Manual:
``` C#
    public void Register()
    {
      SerializerTypes.AddType(
        new BinTypeDescription(typeof(Manual), "Manual")
        , new BinTypeVersion(10, 0)
        , BinTypeProcess.Create<Manual>(Manual.Write, Manual.Read)
      );
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
```

With generics:
``` C#
    public void Register()
    {
      var write = typeof(SerializeTests).GetMethod("Write", BindingFlags.Static | BindingFlags.Public);
      var read = typeof(SerializeTests).GetMethod("Read", BindingFlags.Static | BindingFlags.Public);

      SerializerTypes.AddType(
        new BinTypeDescription(typeof(ManualGenericType<,>), "ManualGenericType")
        , new BinTypeVersion(10, 0)
        , BinTypeProcess.Create(write, read)
      );
    }

    public class ManualGenericType<T1, T2>
    {
      public T1 FieldOne;
      public T2 FieldTwo;
    }

    public static void Write<T1, T2>(Stream stream, ManualGenericType<T1, T2> instance)
    {
      BinSerializer.Serialize(stream, instance.FieldOne);
      BinSerializer.Serialize(stream, instance.FieldTwo);
    }

    public static ManualGenericType<T1, T2> Read<T1, T2>(Stream stream, ManualGenericType<T1, T2> instance, int version)
    {
      instance.FieldOne = BinSerializer.Deserialize<T1>(stream);
      instance.FieldTwo = BinSerializer.Deserialize<T2>(stream);
      return instance;
    }
```

### PerfTest result:
This is a console output from PerfTest project:
```
|                      Method |      Mean |     Error |    StdDev |
|---------------------------- |----------:|----------:|----------:|
|     BinSerializer_Serialize |  6.947 us | 0.0067 us | 0.0060 us |
|   BinaryFormatter_Serialize | 22.222 us | 0.1474 us | 0.1378 us |
|       MessagePack_Serialize |  1.174 us | 0.0104 us | 0.0097 us |
|   BinSerializer_Deserialize |  6.408 us | 0.0405 us | 0.0379 us |
| BinaryFormatter_Deserialize | 30.445 us | 0.0879 us | 0.0822 us |
|     MessagePack_Deserialize |  1.441 us | 0.0044 us | 0.0041 us |
```

### Serialization format:
![Link](https://github.com/Nirklav/BinSerializer/blob/master/Format.md)
