using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
  public enum StreamExtensionKind
  {
    Write,
    Read,
    Skip
  }

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public class BinStreamExtensionAttribute : Attribute
  {
    public Type Type { get; private set; }
    public StreamExtensionKind Kind { get; private set; }

    public BinStreamExtensionAttribute(Type type, StreamExtensionKind kind)
    {
      Type = type;
      Kind = kind;
    }
  }

  public static class BinStreamExtensions
  {
    #region writers
    [SecuritySafeCritical]
    [BinStreamExtension(typeof(bool), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, bool obj)
    {
      BSDebug.TraceStart("WriteBoolean", stream.Position);

      stream.WriteByte(obj ? (byte)1 : (byte)0);

      BSDebug.TraceEnd("WriteBoolean", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(byte), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, byte obj)
    {
      BSDebug.TraceStart("WriteByte", stream.Position);

      stream.WriteByte(obj);

      BSDebug.TraceEnd("WriteByte", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(sbyte), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, sbyte obj)
    {
      BSDebug.TraceStart("WriteSByte", stream.Position);

      stream.WriteByte((byte)obj);

      BSDebug.TraceEnd("WriteSByte", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(short), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void Write(this Stream stream, short obj)
    {
      BSDebug.TraceStart("WriteInt16", stream.Position);

      var objPtr = (byte*)&obj;
      stream.WriteByte(objPtr[0]);
      stream.WriteByte(objPtr[1]);

      BSDebug.TraceEnd("WriteInt16", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(ushort), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void Write(this Stream stream, ushort obj)
    {
      BSDebug.TraceStart("WriteUInt16", stream.Position);

      var objPtr = (byte*)&obj;
      stream.WriteByte(objPtr[0]);
      stream.WriteByte(objPtr[1]);

      BSDebug.TraceEnd("WriteUInt16", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(char), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void Write(this Stream stream, char obj)
    {
      BSDebug.TraceStart("WriteChar", stream.Position);

      var objPtr = (byte*) &obj;
      stream.WriteByte(objPtr[0]);
      stream.WriteByte(objPtr[1]);

      BSDebug.TraceEnd("WriteChar", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(int), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void Write(this Stream stream, int obj)
    {
      BSDebug.TraceStart("WriteInt32", stream.Position);

      var objPtr = (byte*)&obj;
      stream.WriteByte(objPtr[0]);
      stream.WriteByte(objPtr[1]);
      stream.WriteByte(objPtr[2]);
      stream.WriteByte(objPtr[3]);

      BSDebug.TraceEnd("WriteInt32", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(uint), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void Write(this Stream stream, uint obj)
    {
      BSDebug.TraceStart("WriteUInt32", stream.Position);

      var objPtr = (byte*)&obj;
      stream.WriteByte(objPtr[0]);
      stream.WriteByte(objPtr[1]);
      stream.WriteByte(objPtr[2]);
      stream.WriteByte(objPtr[3]);

      BSDebug.TraceEnd("WriteUInt32", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(long), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void Write(this Stream stream, long obj)
    {
      BSDebug.TraceStart("WriteInt64", stream.Position);

      var objPtr = (byte*)&obj;
      stream.WriteByte(objPtr[0]);
      stream.WriteByte(objPtr[1]);
      stream.WriteByte(objPtr[2]);
      stream.WriteByte(objPtr[3]);
      stream.WriteByte(objPtr[4]);
      stream.WriteByte(objPtr[5]);
      stream.WriteByte(objPtr[6]);
      stream.WriteByte(objPtr[7]);

      BSDebug.TraceEnd("WriteInt64", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(ulong), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void Write(this Stream stream, ulong obj)
    {
      BSDebug.TraceStart("WriteUInt64", stream.Position);

      var objPtr = (byte*)&obj;
      stream.WriteByte(objPtr[0]);
      stream.WriteByte(objPtr[1]);
      stream.WriteByte(objPtr[2]);
      stream.WriteByte(objPtr[3]);
      stream.WriteByte(objPtr[4]);
      stream.WriteByte(objPtr[5]);
      stream.WriteByte(objPtr[6]);
      stream.WriteByte(objPtr[7]);

      BSDebug.TraceEnd("WriteUInt64", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(float), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void Write(this Stream stream, float obj)
    {
      BSDebug.TraceStart("WriteSingle", stream.Position);

      var objPtr = (byte*)&obj;
      stream.WriteByte(objPtr[0]);
      stream.WriteByte(objPtr[1]);
      stream.WriteByte(objPtr[2]);
      stream.WriteByte(objPtr[3]);

      BSDebug.TraceEnd("WriteSingle", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(double), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void Write(this Stream stream, double obj)
    {
      BSDebug.TraceStart("WriteDouble", stream.Position);

      var objPtr = (byte*)&obj;
      stream.WriteByte(objPtr[0]);
      stream.WriteByte(objPtr[1]);
      stream.WriteByte(objPtr[2]);
      stream.WriteByte(objPtr[3]);
      stream.WriteByte(objPtr[4]);
      stream.WriteByte(objPtr[5]);
      stream.WriteByte(objPtr[6]);
      stream.WriteByte(objPtr[7]);

      BSDebug.TraceEnd("WriteDouble", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(decimal), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void Write(this Stream stream, decimal obj)
    {
      BSDebug.TraceStart("WriteDecimal", stream.Position);

      var objPtr = (byte*)&obj;
      stream.WriteByte(objPtr[0]);
      stream.WriteByte(objPtr[1]);
      stream.WriteByte(objPtr[2]);
      stream.WriteByte(objPtr[3]);
      stream.WriteByte(objPtr[4]);
      stream.WriteByte(objPtr[5]);
      stream.WriteByte(objPtr[6]);
      stream.WriteByte(objPtr[7]);
      stream.WriteByte(objPtr[8]);
      stream.WriteByte(objPtr[9]);
      stream.WriteByte(objPtr[10]);
      stream.WriteByte(objPtr[11]);
      stream.WriteByte(objPtr[12]);
      stream.WriteByte(objPtr[13]);
      stream.WriteByte(objPtr[14]);
      stream.WriteByte(objPtr[15]);

      BSDebug.TraceEnd("WriteDecimal", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(string), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, string obj)
    {
      BSDebug.TraceStart("WriteString", stream.Position);

      const int useBufferFrom = 4;

      var length = obj.Length;
      var bytesLength = length * sizeof(char);

      // Write length
      stream.Write(length);

      if (length < useBufferFrom)
      {
        unsafe
        {
          fixed (char* objPtr = obj)
          {
            var objBytePtr = (byte*)objPtr;
            for (var i = 0; i < bytesLength; i++)
              stream.WriteByte(objBytePtr[i]);
          }
        }
      }
      else
      {
        var buffer = Buffers.Get(bytesLength);

        unsafe
        {
          fixed (char* valuePtr = obj)
            fixed (byte* bufferPtr = buffer)
              Buffer.MemoryCopy(valuePtr, bufferPtr, buffer.Length, bytesLength);
        }

        // Write data
        stream.Write(buffer, 0, bytesLength);
      }

      BSDebug.TraceEnd("WriteString", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(DateTime), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, DateTime obj)
    {
      BSDebug.TraceStart("WriteDateTime", stream.Position);

      stream.Write(obj.Ticks);

      BSDebug.TraceEnd("WriteDateTime", stream.Position);
    }
    #endregion

    #region readers
    [SecuritySafeCritical]
    [BinStreamExtension(typeof(bool), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadBoolean(this Stream stream)
    {
      BSDebug.TraceStart("ReadBoolean", stream.Position);

      var result = stream.ReadByte() == 1;

      BSDebug.TraceEnd("ReadBoolean", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(byte), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(this Stream stream)
    {
      BSDebug.TraceStart("ReadByte", stream.Position);

      var result = (byte)stream.ReadByte();

      BSDebug.TraceEnd("ReadByte", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(sbyte), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ReadSByte(this Stream stream)
    {
      BSDebug.TraceStart("ReadSByte", stream.Position);

      var result = (sbyte)stream.ReadByte();

      BSDebug.TraceEnd("ReadSByte", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(short), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadInt16(this Stream stream)
    {
      BSDebug.TraceStart("ReadInt16", stream.Position);

      var b1 = stream.ReadByte();
      var b2 = stream.ReadByte();

      var value = b1 | (b2 << 8);

      BSDebug.TraceEnd("ReadInt16", stream.Position);
      return (short)value;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(ushort), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUInt16(this Stream stream)
    {
      BSDebug.TraceStart("ReadUInt16", stream.Position);

      var b1 = stream.ReadByte();
      var b2 = stream.ReadByte();

      var value = b1 | (b2 << 8);

      BSDebug.TraceEnd("ReadUInt16", stream.Position);
      return (ushort)value;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(char), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ReadChar(this Stream stream)
    {
      BSDebug.TraceStart("ReadChar", stream.Position);

      var b1 = stream.ReadByte();
      var b2 = stream.ReadByte();

      var value = b1 | (b2 << 8);

      BSDebug.TraceEnd("ReadChar", stream.Position);
      return (char)value;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(int), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt32(this Stream stream)
    {
      BSDebug.TraceStart("ReadInt32", stream.Position);

      var b1 = stream.ReadByte();
      var b2 = stream.ReadByte();
      var b3 = stream.ReadByte();
      var b4 = stream.ReadByte();

      var value = b1 | (b2 << 8) | (b3 << 16) | (b4 << 24);

      BSDebug.TraceEnd("ReadInt32", stream.Position);
      return value;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(uint), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt32(this Stream stream)
    {
      BSDebug.TraceStart("ReadUInt32", stream.Position);

      var b1 = stream.ReadByte();
      var b2 = stream.ReadByte();
      var b3 = stream.ReadByte();
      var b4 = stream.ReadByte();

      var value = b1 | (b2 << 8) | (b3 << 16) | (b4 << 24);

      BSDebug.TraceEnd("ReadUInt32", stream.Position);
      return (uint)value;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(long), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

      var value = b1 | (b2 << 8) | (b3 << 16) | (b4 << 24) | (b5 << 32) | (b6 << 40) | (b7 << 48) | (b8 << 56);
      BSDebug.TraceEnd("ReadInt64", stream.Position);
      return value;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(ulong), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

      var value = b1 | (b2 << 8) | (b3 << 16) | (b4 << 24) | (b5 << 32) | (b6 << 40) | (b7 << 48) | (b8 << 56);

      BSDebug.TraceEnd("ReadUInt64", stream.Position);
      return (ulong)value;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(float), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ReadSingle(this Stream stream)
    {
      BSDebug.TraceStart("ReadSingle", stream.Position);

      var result = ToSingle(stream.ReadInt32());

      BSDebug.TraceEnd("ReadSingle", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(double), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadDouble(this Stream stream)
    {
      BSDebug.TraceStart("ReadDouble", stream.Position);

      var result = ToDouble(stream.ReadInt64());

      BSDebug.TraceEnd("ReadDouble", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(decimal), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ReadDecimal(this Stream stream)
    {
      BSDebug.TraceStart("ReadDecimal", stream.Position);

      decimal result;
      unsafe
      {
        var p = (long*)&result;
        p[0] = stream.ReadInt64();
        p[1] = stream.ReadInt64();
      }

      BSDebug.TraceEnd("ReadDecimal", stream.Position);
      return result;
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(string), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadString(this Stream stream)
    {
      BSDebug.TraceStart("ReadString", stream.Position);

      var length = stream.ReadInt32();
      var bufferLength = length * sizeof(char);
      var buffer = Buffers.Get(bufferLength);
      stream.Read(buffer, 0, bufferLength);

      BSDebug.TraceEnd("ReadString", stream.Position);
     
      unsafe
      {
        fixed (byte* bufferPtr = buffer)
        {
          var bufferCharPtr = (char*)bufferPtr;
          return new string(bufferCharPtr, 0, length);
        }
      }
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(DateTime), StreamExtensionKind.Read)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ReadDateTime(this Stream stream)
    {
      BSDebug.TraceStart("ReadDateTime", stream.Position);

      var ticks = stream.ReadInt64();

      BSDebug.TraceEnd("ReadDateTime", stream.Position);
      return new DateTime(ticks);
    }
    #endregion

    #region skipers
    [SecuritySafeCritical]
    [BinStreamExtension(typeof(bool), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipBoolean(this Stream stream)
    {
      BSDebug.TraceStart("SkipBoolean", stream.Position);

      stream.Position += sizeof(byte);

      BSDebug.TraceEnd("SkipBoolean", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(byte), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipByte(this Stream stream)
    {
      BSDebug.TraceStart("SkipByte", stream.Position);

      stream.Position += sizeof(byte);

      BSDebug.TraceEnd("SkipByte", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(sbyte), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipSByte(this Stream stream)
    {
      BSDebug.TraceStart("SkipSByte", stream.Position);

      stream.Position += sizeof(byte);

      BSDebug.TraceEnd("SkipSByte", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(short), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipInt16(this Stream stream)
    {
      BSDebug.TraceStart("SkipInt16", stream.Position);

      stream.Position += sizeof(short);

      BSDebug.TraceEnd("SkipInt16", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(ushort), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipUInt16(this Stream stream)
    {
      BSDebug.TraceStart("SkipUInt16", stream.Position);

      stream.Position += sizeof(short);

      BSDebug.TraceEnd("SkipUInt16", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(char), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipChar(this Stream stream)
    {
      BSDebug.TraceStart("SkipChar", stream.Position);

      stream.Position += sizeof(short);

      BSDebug.TraceEnd("SkipChar", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(int), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipInt32(this Stream stream)
    {
      BSDebug.TraceStart("SkipInt32", stream.Position);

      stream.Position += sizeof(int);

      BSDebug.TraceEnd("SkipInt32", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(uint), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipUInt32(this Stream stream)
    {
      BSDebug.TraceStart("SkipUInt32", stream.Position);

      stream.Position += sizeof(int);

      BSDebug.TraceEnd("SkipUInt32", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(long), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipInt64(this Stream stream)
    {
      BSDebug.TraceStart("SkipInt64", stream.Position);

      stream.Position += sizeof(long);

      BSDebug.TraceEnd("SkipInt64", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(ulong), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipUInt64(this Stream stream)
    {
      BSDebug.TraceStart("SkipUInt64", stream.Position);

      stream.Position += sizeof(long);

      BSDebug.TraceEnd("SkipUInt64", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(float), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipSingle(this Stream stream)
    {
      BSDebug.TraceStart("SkipSingle", stream.Position);

      stream.Position += sizeof(int);

      BSDebug.TraceEnd("SkipSingle", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(double), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipDouble(this Stream stream)
    {
      BSDebug.TraceStart("SkipDouble", stream.Position);

      stream.Position += sizeof(long);

      BSDebug.TraceEnd("SkipDouble", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(decimal), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipDecimal(this Stream stream)
    {
      BSDebug.TraceStart("SkipDecimal", stream.Position);

      stream.Position += sizeof(long) * 2;

      BSDebug.TraceEnd("SkipDecimal", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(string), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipString(this Stream stream)
    {
      BSDebug.TraceStart("SkipString", stream.Position);

      var strLength = stream.ReadInt32();
      stream.Position += strLength * sizeof(char);

      BSDebug.TraceEnd("SkipString", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(DateTime), StreamExtensionKind.Skip)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipDateTime(this Stream stream)
    {
      BSDebug.TraceStart("SkipDateTime", stream.Position);

      stream.Position += sizeof(long);

      BSDebug.TraceEnd("SkipDateTime", stream.Position);
    }
    #endregion

    #region helpers
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int ToInt(float f)
    {
      return *(int*)(&f);
    }

    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe long ToLong(double f)
    {
      return *(long*)(&f);
    }

    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe float ToSingle(int i)
    {
      return *(float*)(&i);
    }

    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe double ToDouble(long i)
    {
      return *(double*)(&i);
    }
    #endregion
  }

  internal static class Buffers
  {
    private const int MaxSize = 1024 * 2;

    [ThreadStatic]
    private static byte[] _buffer;

    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Get(int size)
    {
      if (size > MaxSize)
        return new byte[size];
      var buffer = _buffer; // Get thread static field oly once per call
      if (buffer == null)
        _buffer = buffer = new byte[MaxSize];
      return buffer;
    }
  }
}
