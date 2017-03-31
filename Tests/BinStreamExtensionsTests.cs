using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThirtyNineEighty.BinarySerializer;

namespace Tests
{
  [TestClass]
  public class BinStreamExtensionsTests
  {
    private readonly Dictionary<Type, MethodInfo> _readers = new Dictionary<Type, MethodInfo>();
    private readonly Dictionary<Type, MethodInfo> _writers = new Dictionary<Type, MethodInfo>();
    private readonly Dictionary<Type, MethodInfo> _skipers = new Dictionary<Type, MethodInfo>();

    [TestInitialize]
    public void Init()
    {
      foreach (var method in typeof(BinStreamExtensions).GetMethods())
      {
        var attribute = method.GetCustomAttribute<BinStreamExtensionAttribute>();
        if (attribute == null)
          continue;

        switch (attribute.Kind)
        {
          case StreamExtensionKind.Write:
            _writers.Add(attribute.Type, method);
            break;
          case StreamExtensionKind.Read:
            _readers.Add(attribute.Type, method);
            break;
          case StreamExtensionKind.Skip:
            _skipers.Add(attribute.Type, method);
            break;
        }
      }
    }

    [TestMethod]
    public void TestWriteRead()
    {
      TestWriteRead(true);
      TestWriteRead<byte>(230);
      TestWriteRead<sbyte>(120);
      TestWriteRead<short>(30000);
      TestWriteRead<ushort>(50000);
      TestWriteRead('s');
      TestWriteRead(int.MaxValue / 2);
      TestWriteRead(uint.MaxValue / 2);
      TestWriteRead(long.MaxValue / 2);
      TestWriteRead(ulong.MaxValue / 2);
      TestWriteRead(float.MaxValue / 2);
      TestWriteRead(double.MaxValue / 2);
      TestWriteRead(decimal.MaxValue / 2);
      TestWriteRead("kek");
      TestWriteRead(DateTime.UtcNow);
    }

    [TestMethod]
    public void TestWriteSkip()
    {
      TestWriteSkip(true);
      TestWriteSkip<byte>(230);
      TestWriteSkip<sbyte>(120);
      TestWriteSkip<short>(30000);
      TestWriteSkip<ushort>(50000);
      TestWriteSkip('s');
      TestWriteSkip(int.MaxValue / 2);
      TestWriteSkip(uint.MaxValue / 2);
      TestWriteSkip(long.MaxValue / 2);
      TestWriteSkip(ulong.MaxValue / 2);
      TestWriteSkip(float.MaxValue / 2);
      TestWriteSkip(double.MaxValue / 2);
      TestWriteSkip(decimal.MaxValue / 2);
      TestWriteSkip("kek");
      TestWriteSkip(DateTime.UtcNow);
    }

    private void TestWriteRead<T>(T input)
    {
      var type = typeof(T);
      var stream = new MemoryStream();

      var writer = _writers[type];
      var reader = _readers[type];

      writer.Invoke(null, new object[] { stream, input });
      stream.Position = 0;
      var output = (T)reader.Invoke(null, new object[] { stream });

      Assert.AreEqual(input, output, string.Format("Error for {0} type. Writed {1}, Readed {2}", type.Name, input, output));
    }

    private void TestWriteSkip<T>(T input)
    {
      var type = typeof(T);
      var stream = new MemoryStream();

      var writer = _writers[type];
      var skip = _skipers[type];

      writer.Invoke(null, new object[] { stream, input });
      var prevPos = stream.Position;
      stream.Position = 0;
      skip.Invoke(null, new object[] { stream });

      Assert.AreEqual(prevPos, stream.Position, string.Format("Error for {0} type", type.Name));
    }
  }
}
