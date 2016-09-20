using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace ThirtyNineEighty.BinSerializer
{
  public static class BSDebug
  {
    private const string TraceFile = @"D:\trace.txt";

    [Conditional("DEBUG")]
    public static void TraceStart(string methodName)
    {
      WriteImpl($"Type start { methodName }\r\n");
    }

    [Conditional("DEBUG")]
    public static void TraceEnd(string methodName)
    {
      WriteImpl($"Type end { methodName }\r\n");
    }

    [Conditional("DEBUG")]
    public static void TraceStart(ILGenerator il, string methodName)
    {
      Write(il, $"Type start { methodName }\r\n");
    }

    [Conditional("DEBUG")]
    public static void TraceEnd(ILGenerator il, string methodName)
    {
      Write(il, $"Type end { methodName }\r\n");
    }

    [Conditional("DEBUG")]
    private static void Write(ILGenerator il, string text)
    {
      il.Emit(OpCodes.Ldstr, text);
      il.Emit(OpCodes.Call, typeof(BSDebug).GetMethod(nameof(BSDebug.WriteImpl), BindingFlags.Static | BindingFlags.NonPublic));
    }

    [Conditional("DEBUG")]
    public static void TraceStart(string methodName, long pos)
    {
      WriteImpl($"Start { methodName } stream pos { pos }\r\n");
    }

    [Conditional("DEBUG")]
    public static void TraceEnd(string methodName, long pos)
    {
      WriteImpl($"End { methodName } stream pos { pos }\r\n");
    }

    [Conditional("DEBUG")]
    private static void WriteImpl(string text)
    {
      //File.AppendAllText(TraceFile, text);
    }
  }
}
