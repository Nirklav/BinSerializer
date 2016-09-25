using System;
using System.IO;
using System.Security;
using System.Text;

namespace ThirtyNineEighty.BinarySerializer
{
  enum StreamExtensionKind
  {
    Write,
    Read,
    Skip
  }

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  class StreamExtensionAttribute : Attribute
  {
    public Type Type { get; private set; }
    public StreamExtensionKind Kind { get; private set; }

    public StreamExtensionAttribute(Type type, StreamExtensionKind kind)
    {
      Type = type;
      Kind = kind;
    }
  }

  public static class StreamExtensions
  {
    private static readonly DateTime _unixEpochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    #region writers
    [SecuritySafeCritical]
    [StreamExtension(typeof(bool), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, bool obj)
    {
      BSDebug.TraceStart("WriteBoolean", stream.Position);

      stream.WriteByte(obj ? (byte)1 : (byte)0);

      BSDebug.TraceEnd("WriteBoolean", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(byte), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, byte obj)
    {
      BSDebug.TraceStart("WriteByte", stream.Position);

      stream.WriteByte(obj);

      BSDebug.TraceEnd("WriteByte", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(sbyte), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, sbyte obj)
    {
      BSDebug.TraceStart("WriteSByte", stream.Position);

      stream.WriteByte((byte)obj);

      BSDebug.TraceEnd("WriteSByte", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(short), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, short obj)
    {
      BSDebug.TraceStart("WriteInt16", stream.Position);

      var b1 = (byte)(obj & 0xFF);
      var b2 = (byte)((obj & (0xFF << 8)) >> 8);

      stream.WriteByte(b1);
      stream.WriteByte(b2);

      BSDebug.TraceEnd("WriteInt16", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(ushort), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, ushort obj)
    {
      BSDebug.TraceStart("WriteUInt16", stream.Position);

      var b1 = (byte)(obj & 0xFF);
      var b2 = (byte)((obj & (0xFF << 8)) >> 8);

      stream.WriteByte(b1);
      stream.WriteByte(b2);

      BSDebug.TraceEnd("WriteUInt16", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(char), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, char obj)
    {
      BSDebug.TraceStart("WriteChar", stream.Position);

      var b1 = (byte)(obj & 0xFF);
      var b2 = (byte)((obj & (0xFF << 8)) >> 8);

      stream.WriteByte(b1);
      stream.WriteByte(b2);

      BSDebug.TraceEnd("WriteChar", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(int), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, int obj)
    {
      BSDebug.TraceStart("WriteInt32", stream.Position);

      var b1 = (byte)(obj & 0xFF);
      var b2 = (byte)((obj & (0xFF << 8)) >> 8);
      var b3 = (byte)((obj & (0xFF << 16)) >> 16);
      var b4 = (byte)((obj & (0xFF << 24)) >> 24);

      stream.WriteByte(b1);
      stream.WriteByte(b2);
      stream.WriteByte(b3);
      stream.WriteByte(b4);

      BSDebug.TraceEnd("WriteInt32", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(uint), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, uint obj)
    {
      BSDebug.TraceStart("WriteUInt32", stream.Position);

      var b1 = (byte)(obj & 0xFF);
      var b2 = (byte)((obj & (0xFF << 8)) >> 8);
      var b3 = (byte)((obj & (0xFF << 16)) >> 16);
      var b4 = (byte)((obj & (0xFF << 24)) >> 24);

      stream.WriteByte(b1);
      stream.WriteByte(b2);
      stream.WriteByte(b3);
      stream.WriteByte(b4);

      BSDebug.TraceEnd("WriteUInt32", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(long), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, long obj)
    {
      BSDebug.TraceStart("WriteInt64", stream.Position);

      var b1 = (byte)(obj & 0xFFL);
      var b2 = (byte)((obj & (0xFFL << 8)) >> 8);
      var b3 = (byte)((obj & (0xFFL << 16)) >> 16);
      var b4 = (byte)((obj & (0xFFL << 24)) >> 24);
      var b5 = (byte)((obj & (0xFFL << 32)) >> 32);
      var b6 = (byte)((obj & (0xFFL << 40)) >> 40);
      var b7 = (byte)((obj & (0xFFL << 48)) >> 48);
      var b8 = (byte)((obj & (0xFFL << 56)) >> 56);

      stream.WriteByte(b1);
      stream.WriteByte(b2);
      stream.WriteByte(b3);
      stream.WriteByte(b4);
      stream.WriteByte(b5);
      stream.WriteByte(b6);
      stream.WriteByte(b7);
      stream.WriteByte(b8);

      BSDebug.TraceEnd("WriteInt64", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(ulong), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, ulong obj)
    {
      BSDebug.TraceStart("WriteUInt64", stream.Position);

      var objL = (long)obj;
      var b1 = (byte)(objL & 0xFFL);
      var b2 = (byte)((objL & (0xFFL << 8)) >> 8);
      var b3 = (byte)((objL & (0xFFL << 16)) >> 16);
      var b4 = (byte)((objL & (0xFFL << 24)) >> 24);
      var b5 = (byte)((objL & (0xFFL << 32)) >> 32);
      var b6 = (byte)((objL & (0xFFL << 40)) >> 40);
      var b7 = (byte)((objL & (0xFFL << 48)) >> 48);
      var b8 = (byte)((objL & (0xFFL << 56)) >> 56);

      stream.WriteByte(b1);
      stream.WriteByte(b2);
      stream.WriteByte(b3);
      stream.WriteByte(b4);
      stream.WriteByte(b5);
      stream.WriteByte(b6);
      stream.WriteByte(b7);
      stream.WriteByte(b8);

      BSDebug.TraceEnd("WriteUInt64", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(float), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, float obj)
    {
      BSDebug.TraceStart("WriteSingle", stream.Position);

      stream.Write(ToInt(obj));

      BSDebug.TraceEnd("WriteSingle", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(double), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, double obj)
    {
      BSDebug.TraceStart("WriteDouble", stream.Position);

      stream.Write(ToLong(obj));

      BSDebug.TraceEnd("WriteDouble", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(decimal), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, decimal obj)
    {
      BSDebug.TraceStart("WriteDecimal", stream.Position);

      stream.Write(ToLong(obj));

      BSDebug.TraceEnd("WriteDecimal", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(string), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, string obj)
    {
      BSDebug.TraceStart("WriteString", stream.Position);

      stream.Write(obj.Length);
      foreach (var ch in obj)
        stream.Write(ch);

      BSDebug.TraceEnd("WriteString", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(DateTime), StreamExtensionKind.Write)]
    public static void Write(this Stream stream, DateTime obj)
    {
      BSDebug.TraceStart("WriteDateTime", stream.Position);

      var seconds = obj - _unixEpochStart;
      stream.Write((long)seconds.TotalSeconds);

      BSDebug.TraceEnd("WriteDateTime", stream.Position);
    }
    #endregion

    #region readers
    [SecuritySafeCritical]
    [StreamExtension(typeof(bool), StreamExtensionKind.Read)]
    public static bool ReadBoolean(this Stream stream)
    {
      BSDebug.TraceStart("ReadBoolean", stream.Position);

      var result = stream.ReadByte() == 1;

      BSDebug.TraceEnd("ReadBoolean", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(byte), StreamExtensionKind.Read)]
    public static byte ReadByte(this Stream stream)
    {
      BSDebug.TraceStart("ReadByte", stream.Position);

      var result = (byte)stream.ReadByte();

      BSDebug.TraceEnd("ReadByte", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(sbyte), StreamExtensionKind.Read)]
    public static sbyte ReadSByte(this Stream stream)
    {
      BSDebug.TraceStart("ReadSByte", stream.Position);

      var result = (sbyte)stream.ReadByte();

      BSDebug.TraceEnd("ReadSByte", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(short), StreamExtensionKind.Read)]
    public static short ReadInt16(this Stream stream)
    {
      BSDebug.TraceStart("ReadInt16", stream.Position);

      var b1 = stream.ReadByte();
      var b2 = stream.ReadByte();

      var value = 0;
      value |= b1;
      value |= (b2 << 8);

      BSDebug.TraceEnd("ReadInt16", stream.Position);
      return (short)value;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(ushort), StreamExtensionKind.Read)]
    public static ushort ReadUInt16(this Stream stream)
    {
      BSDebug.TraceStart("ReadUInt16", stream.Position);

      var b1 = stream.ReadByte();
      var b2 = stream.ReadByte();

      var value = 0;
      value |= b1;
      value |= (b2 << 8);

      BSDebug.TraceEnd("ReadUInt16", stream.Position);
      return (ushort)value;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(char), StreamExtensionKind.Read)]
    public static char ReadChar(this Stream stream)
    {
      BSDebug.TraceStart("ReadChar", stream.Position);

      var b1 = stream.ReadByte();
      var b2 = stream.ReadByte();

      var value = 0;
      value |= b1;
      value |= (b2 << 8);

      BSDebug.TraceEnd("ReadChar", stream.Position);
      return (char)value;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(int), StreamExtensionKind.Read)]
    public static int ReadInt32(this Stream stream)
    {
      BSDebug.TraceStart("ReadInt32", stream.Position);

      var b1 = stream.ReadByte();
      var b2 = stream.ReadByte();
      var b3 = stream.ReadByte();
      var b4 = stream.ReadByte();

      var value = 0;
      value |= b1;
      value |= (b2 << 8);
      value |= (b3 << 16);
      value |= (b4 << 24);

      BSDebug.TraceEnd("ReadInt32", stream.Position);
      return value;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(uint), StreamExtensionKind.Read)]
    public static uint ReadUInt32(this Stream stream)
    {
      BSDebug.TraceStart("ReadUInt32", stream.Position);

      var b1 = stream.ReadByte();
      var b2 = stream.ReadByte();
      var b3 = stream.ReadByte();
      var b4 = stream.ReadByte();

      var value = 0;
      value |= b1;
      value |= (b2 << 8);
      value |= (b3 << 16);
      value |= (b4 << 24);

      BSDebug.TraceEnd("ReadUInt32", stream.Position);
      return (uint)value;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(long), StreamExtensionKind.Read)]
    public static long ReadInt64(this Stream stream)
    {
      BSDebug.TraceStart("ReadInt64", stream.Position);

      var b1 = (long)stream.ReadByte();
      var b2 = (long)stream.ReadByte();
      var b3 = (long)stream.ReadByte();
      var b4 = (long)stream.ReadByte();
      var b5 = (long)stream.ReadByte();
      var b6 = (long)stream.ReadByte();
      var b7 = (long)stream.ReadByte();
      var b8 = (long)stream.ReadByte();

      var value = 0L;
      value |= b1;
      value |= (b2 << 8);
      value |= (b3 << 16);
      value |= (b4 << 24);
      value |= (b5 << 32);
      value |= (b6 << 40);
      value |= (b7 << 48);
      value |= (b8 << 56);

      BSDebug.TraceEnd("ReadInt64", stream.Position);
      return value;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(ulong), StreamExtensionKind.Read)]
    public static ulong ReadUInt64(this Stream stream)
    {
      BSDebug.TraceStart("ReadUInt64", stream.Position);

      var b1 = (long)stream.ReadByte();
      var b2 = (long)stream.ReadByte();
      var b3 = (long)stream.ReadByte();
      var b4 = (long)stream.ReadByte();
      var b5 = (long)stream.ReadByte();
      var b6 = (long)stream.ReadByte();
      var b7 = (long)stream.ReadByte();
      var b8 = (long)stream.ReadByte();

      var value = 0L;
      value |= b1;
      value |= (b2 << 8);
      value |= (b3 << 16);
      value |= (b4 << 24);
      value |= (b5 << 32);
      value |= (b6 << 40);
      value |= (b7 << 48);
      value |= (b8 << 56);

      BSDebug.TraceEnd("ReadUInt64", stream.Position);
      return (ulong)value;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(float), StreamExtensionKind.Read)]
    public static float ReadSingle(this Stream stream)
    {
      BSDebug.TraceStart("ReadSingle", stream.Position);

      var result = ToSingle(stream.ReadInt32());

      BSDebug.TraceEnd("ReadSingle", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(double), StreamExtensionKind.Read)]
    public static double ReadDouble(this Stream stream)
    {
      BSDebug.TraceStart("ReadDouble", stream.Position);

      var result = ToDouble(stream.ReadInt64());

      BSDebug.TraceEnd("ReadDouble", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(decimal), StreamExtensionKind.Read)]
    public static decimal ReadDecimal(this Stream stream)
    {
      BSDebug.TraceStart("ReadDecimal", stream.Position);

      var result = ToDecimal(stream.ReadInt64());

      BSDebug.TraceEnd("ReadDecimal", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(string), StreamExtensionKind.Read)]
    public static string ReadString(this Stream stream)
    {
      BSDebug.TraceStart("ReadString", stream.Position);

      var length = stream.ReadInt32();
      var builder = new StringBuilder(length);
      for (int i = 0; i < length; i++)
        builder.Append(stream.ReadChar());

      BSDebug.TraceEnd("ReadString", stream.Position);
      return builder.ToString();
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(DateTime), StreamExtensionKind.Read)]
    public static DateTime ReadDateTime(this Stream stream)
    {
      BSDebug.TraceStart("ReadDateTime", stream.Position);

      var unixTime = stream.ReadInt64();

      BSDebug.TraceEnd("ReadDateTime", stream.Position);
      return _unixEpochStart.AddSeconds(unixTime);
    }
    #endregion

    #region skipers
    [SecuritySafeCritical]
    [StreamExtension(typeof(bool), StreamExtensionKind.Skip)]
    public static void SkipBoolean(this Stream stream)
    {
      BSDebug.TraceStart("SkipBoolean", stream.Position);

      stream.Position += sizeof(byte);

      BSDebug.TraceEnd("SkipBoolean", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(byte), StreamExtensionKind.Skip)]
    public static void SkipByte(this Stream stream)
    {
      BSDebug.TraceStart("SkipByte", stream.Position);

      stream.Position += sizeof(byte);

      BSDebug.TraceEnd("SkipByte", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(sbyte), StreamExtensionKind.Skip)]
    public static void SkipSByte(this Stream stream)
    {
      BSDebug.TraceStart("SkipSByte", stream.Position);

      stream.Position += sizeof(byte);

      BSDebug.TraceEnd("SkipSByte", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(short), StreamExtensionKind.Skip)]
    public static void SkipInt16(this Stream stream)
    {
      BSDebug.TraceStart("SkipInt16", stream.Position);

      stream.Position += sizeof(short);

      BSDebug.TraceEnd("SkipInt16", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(ushort), StreamExtensionKind.Skip)]
    public static void SkipUInt16(this Stream stream)
    {
      BSDebug.TraceStart("SkipUInt16", stream.Position);

      stream.Position += sizeof(short);

      BSDebug.TraceEnd("SkipUInt16", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(char), StreamExtensionKind.Skip)]
    public static void SkipChar(this Stream stream)
    {
      BSDebug.TraceStart("SkipChar", stream.Position);

      stream.Position += sizeof(short);

      BSDebug.TraceEnd("SkipChar", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(int), StreamExtensionKind.Skip)]
    public static void SkipInt32(this Stream stream)
    {
      BSDebug.TraceStart("SkipInt32", stream.Position);

      stream.Position += sizeof(int);

      BSDebug.TraceEnd("SkipInt32", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(uint), StreamExtensionKind.Skip)]
    public static void SkipUInt32(this Stream stream)
    {
      BSDebug.TraceStart("SkipUInt32", stream.Position);

      stream.Position += sizeof(int);

      BSDebug.TraceEnd("SkipUInt32", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(long), StreamExtensionKind.Skip)]
    public static void SkipInt64(this Stream stream)
    {
      BSDebug.TraceStart("SkipInt64", stream.Position);

      stream.Position += sizeof(long);

      BSDebug.TraceEnd("SkipInt64", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(ulong), StreamExtensionKind.Skip)]
    public static void SkipUInt64(this Stream stream)
    {
      BSDebug.TraceStart("SkipUInt64", stream.Position);

      stream.Position += sizeof(long);

      BSDebug.TraceEnd("SkipUInt64", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(float), StreamExtensionKind.Skip)]
    public static void SkipSingle(this Stream stream)
    {
      BSDebug.TraceStart("SkipSingle", stream.Position);

      stream.Position += sizeof(int);

      BSDebug.TraceEnd("SkipSingle", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(double), StreamExtensionKind.Skip)]
    public static void SkipDouble(this Stream stream)
    {
      BSDebug.TraceStart("SkipDouble", stream.Position);

      stream.Position += sizeof(long);

      BSDebug.TraceEnd("SkipDouble", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(decimal), StreamExtensionKind.Skip)]
    public static void SkipDecimal(this Stream stream)
    {
      BSDebug.TraceStart("SkipDecimal", stream.Position);

      stream.Position += sizeof(byte);

      BSDebug.TraceEnd("SkipDecimal", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(string), StreamExtensionKind.Skip)]
    public static void SkipString(this Stream stream)
    {
      BSDebug.TraceStart("SkipString", stream.Position);

      var strLength = stream.ReadInt32();
      stream.Position += strLength * sizeof(char);

      BSDebug.TraceEnd("SkipString", stream.Position);
    }

    [SecuritySafeCritical]
    [StreamExtension(typeof(DateTime), StreamExtensionKind.Skip)]
    public static void SkipDateTime(this Stream stream)
    {
      BSDebug.TraceStart("SkipDateTime", stream.Position);

      stream.Position += sizeof(long);

      BSDebug.TraceEnd("SkipDateTime", stream.Position);
    }
    #endregion

    #region helpers
    [SecuritySafeCritical]
    private static unsafe int ToInt(float f)
    {
      return *(int*)(&f);
    }

    [SecuritySafeCritical]
    private static unsafe long ToLong(double f)
    {
      return *(long*)(&f);
    }

    [SecuritySafeCritical]
    private static unsafe long ToLong(decimal f)
    {
      return *(long*)(&f);
    }

    [SecuritySafeCritical]
    private static unsafe float ToSingle(int i)
    {
      return *(float*)(&i);
    }

    [SecuritySafeCritical]
    private static unsafe double ToDouble(long i)
    {
      return *(double*)(&i);
    }

    [SecuritySafeCritical]
    private static unsafe decimal ToDecimal(long i)
    {
      return *(decimal*)(&i);
    }
    #endregion
  }
}
