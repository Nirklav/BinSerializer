## Binary serializer
- Inheritance support.
- Generics support.
- References supprot.
- Backward compatibility (with versions).
- ValueTypes serialized and deserialized without boxing (Except for the case when type versions not equals).
- Two times faster than BinaryFormatter.
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

Version should be increased when fields added or removed from type.

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
Bin: 3667ms
Formatter: 7523ms
Bin: 3505ms
Formatter: 7372ms
Bin: 3450ms
Formatter: 7327ms
Bin: 3453ms
Formatter: 7347ms
Bin: 3446ms
Formatter: 7356ms
Bin: 3448ms
Formatter: 7318ms
Bin: 3450ms
Formatter: 7356ms
Bin: 3454ms
Formatter: 7332ms
Bin: 3454ms
Formatter: 7355ms
Bin: 3448ms
Formatter: 7379ms
```

### Serialization format:
![Link](https://github.com/Nirklav/BinSerializer/blob/master/Format.md)
