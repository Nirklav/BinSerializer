using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
  static class BSDebug
  {
    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    public static void TraceStart(string methodName)
    {
      WriteImpl(string.Format("Type start {0}\r\n", methodName));
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    public static void TraceEnd(string methodName)
    {
      WriteImpl(string.Format("Type end {0}\r\n", methodName));
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    public static void TraceStart(ILGenerator il, string methodName)
    {
      Write(il, string.Format("Type start {0}\r\n", methodName));
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    public static void TraceEnd(ILGenerator il, string methodName)
    {
      Write(il, string.Format("Type end {0}\r\n", methodName));
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    private static void Write(ILGenerator il, string text)
    {
      il.Emit(OpCodes.Ldstr, text);
      il.Emit(OpCodes.Call, typeof(BSDebug).GetTypeInfo().GetMethod("WriteImpl", BindingFlags.Static | BindingFlags.NonPublic));
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    public static void TraceStart(string methodName, long pos)
    {
      WriteImpl(string.Format("Start {0} stream pos {1}\r\n", methodName, pos));
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    public static void TraceEnd(string methodName, long pos)
    {
      WriteImpl(string.Format("End {0} stream pos {1}\r\n", methodName, pos));
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    private static void WriteImpl(string text)
    {
      //const string traceFile = @"D:\trace.txt";
      //System.IO.File.AppendAllText(traceFile, text);
    }
  }
}
