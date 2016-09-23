using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace ThirtyNineEighty.BinarySerializer
{
  static class BSDebug
  {
    [Conditional("DEBUG")]
    public static void TraceStart(string methodName)
    {
      WriteImpl(string.Format("Type start {0}\r\n", methodName));
    }

    [Conditional("DEBUG")]
    public static void TraceEnd(string methodName)
    {
      WriteImpl(string.Format("Type end {0}\r\n", methodName));
    }

    [Conditional("DEBUG")]
    public static void TraceStart(ILGenerator il, string methodName)
    {
      Write(il, string.Format("Type start {0}\r\n", methodName));
    }

    [Conditional("DEBUG")]
    public static void TraceEnd(ILGenerator il, string methodName)
    {
      Write(il, string.Format("Type end {0}\r\n", methodName));
    }

    [Conditional("DEBUG")]
    private static void Write(ILGenerator il, string text)
    {
      il.Emit(OpCodes.Ldstr, text);
      il.Emit(OpCodes.Call, typeof(BSDebug).GetMethod("WriteImpl", BindingFlags.Static | BindingFlags.NonPublic));
    }

    [Conditional("DEBUG")]
    public static void TraceStart(string methodName, long pos)
    {
      WriteImpl(string.Format("Start {0} stream pos {1}\r\n", methodName, pos));
    }

    [Conditional("DEBUG")]
    public static void TraceEnd(string methodName, long pos)
    {
      WriteImpl(string.Format("End {0} stream pos {1}\r\n", methodName, pos));
    }

    [Conditional("DEBUG")]
    private static void WriteImpl(string text)
    {
      //const string traceFile = @"D:\trace.txt";
      //File.AppendAllText(traceFile, text);
    }
  }
}
