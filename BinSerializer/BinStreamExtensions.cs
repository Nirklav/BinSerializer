using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
  enum StreamExtensionKind
  {
    Write,
    Read,
    Skip
  }

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  class BinStreamExtensionAttribute : Attribute
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
    private static readonly DateTime UnixEpochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
    [BinStreamExtension(typeof(ushort), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [BinStreamExtension(typeof(char), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [BinStreamExtension(typeof(int), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [BinStreamExtension(typeof(uint), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [BinStreamExtension(typeof(long), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [BinStreamExtension(typeof(ulong), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [BinStreamExtension(typeof(float), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, float obj)
    {
      BSDebug.TraceStart("WriteSingle", stream.Position);

      stream.Write(ToInt(obj));

      BSDebug.TraceEnd("WriteSingle", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(double), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, double obj)
    {
      BSDebug.TraceStart("WriteDouble", stream.Position);

      stream.Write(ToLong(obj));

      BSDebug.TraceEnd("WriteDouble", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(decimal), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, decimal obj)
    {
      BSDebug.TraceStart("WriteDecimal", stream.Position);

      stream.Write(ToLong(obj));

      BSDebug.TraceEnd("WriteDecimal", stream.Position);
    }

    [SecuritySafeCritical]
    [BinStreamExtension(typeof(string), StreamExtensionKind.Write)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, string obj)
    {
      BSDebug.TraceStart("WriteString", stream.Position);

      const int useBufferFrom = 7;

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
            var valueBytePtr = (byte*)objPtr;
            for (int i = 0; i < bytesLength; i++)
              stream.WriteByte(valueBytePtr[i]);
          }
        }
      }
      else
      {
        var buffer = Buffers.Get(bytesLength);
        unsafe
        {
          // It work faster but unfortunately available only from .net framework 4.6
          //fixed (char* valuePtr = value)
          //  fixed (byte* bufferPtr = buffer)
          //    Buffer.MemoryCopy(valuePtr, bufferPtr, buffer.Length, usedLength);

          fixed (void* objPtr = obj)
            Marshal.Copy(new IntPtr(objPtr), buffer, 0, bytesLength);
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

      var seconds = obj - UnixEpochStart;
      stream.Write((long)seconds.TotalSeconds);

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

      var result = ToDecimal(stream.ReadInt64());

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

      var unixTime = stream.ReadInt64();

      BSDebug.TraceEnd("ReadDateTime", stream.Position);
      return UnixEpochStart.AddSeconds(unixTime);
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

      stream.Position += sizeof(byte);

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
    private static unsafe long ToLong(decimal f)
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

    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe decimal ToDecimal(long i)
    {
      return *(decimal*)(&i);
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
