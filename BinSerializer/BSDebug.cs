﻿using System.Diagnostics;
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
      WriteImpl($"Type start { methodName }\r\n");
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    public static void TraceEnd(string methodName)
    {
      WriteImpl($"Type end { methodName }\r\n");
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    public static void TraceStart(ILGenerator il, string methodName)
    {
      Write(il, $"Type start { methodName }\r\n");
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    public static void TraceEnd(ILGenerator il, string methodName)
    {
      Write(il, $"Type end { methodName }\r\n");
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
      WriteImpl($"Start { methodName } stream pos { pos }\r\n");
    }

    [Conditional("DEBUG")]
    [SecuritySafeCritical]
    public static void TraceEnd(string methodName, long pos)
    {
      WriteImpl($"End { methodName } stream pos { pos }\r\n");
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
