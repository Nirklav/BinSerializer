using System;
using System.IO;
using System.Reflection;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  public delegate void BinWriter<in T>(Stream stream, T obj);
  public delegate T BinReader<T>(Stream stream, T obj, int version);

  public sealed class BinTypeProcess
  {
    internal readonly MethodInfo StreamWriter;
    internal readonly MethodInfo StreamReader;
    internal readonly MethodInfo StreamSkiper;

    internal readonly MethodInfo TypeWriter;
    internal readonly MethodInfo TypeReader;

    public static BinTypeProcess Create<T>(BinWriter<T> writer, BinReader<T> reader)
    {
      return new BinTypeProcess(null, null, null, writer.GetMethodInfo(), reader.GetMethodInfo());
    }

    public static BinTypeProcess Create(MethodInfo writer, MethodInfo reader)
    {
      return new BinTypeProcess(null, null, null, writer, reader);
    }

    internal static BinTypeProcess CreateStreamProcess(MethodInfo streamWriter, MethodInfo streamReader, MethodInfo streamSkiper)
    {
      return new BinTypeProcess(streamWriter, streamReader, streamSkiper, null, null);
    }

    private BinTypeProcess(MethodInfo streamWriter, MethodInfo streamReader, MethodInfo streamSkiper, MethodInfo typeWriter, MethodInfo typeReader)
    {
      if (streamWriter != null)
        ValidateStreamWriter(streamWriter);
      if (streamReader != null)
        ValidateStreamReader(streamReader);
      if (streamSkiper != null)
        ValidateStreamSkiper(streamSkiper);
      if (typeWriter != null)
        ValidateTypeWriter(typeWriter);
      if (typeReader != null)
        ValidateTypeReader(typeReader);

      StreamWriter = streamWriter;
      StreamReader = streamReader;
      StreamSkiper = streamSkiper;
      TypeWriter = typeWriter;
      TypeReader = typeReader;
    }

    private static void ValidateStreamWriter(MethodInfo method)
    {
      if (method.DeclaringType != typeof(BinStreamExtensions))
        throw new InvalidOperationException("Method must be declared at BinStreamExtensions class only");

      ValidateTypeWriter(method);
    }

    private static void ValidateTypeWriter(MethodInfo method)
    {
      if (!method.IsStatic)
        throw new ArgumentException("Writer method must be static.");

      var parameters = method.GetParameters();
      if (parameters.Length != 2)
        throw new ArgumentException("Writer has invalid parameters count. Method must have 2 parameters.");

      if (parameters[0].ParameterType != typeof(Stream))
        throw new ArgumentException("Writer has invalid parameters. First parameter must be Stream.");

      if (method.ReturnType != typeof(void))
        throw new ArgumentException("Writer has invalid return type. Method must return nothing.");
    }

    private static void ValidateStreamReader(MethodInfo method)
    {
      if (!method.IsStatic)
        throw new ArgumentException("Reader method must be static.");

      if (method.DeclaringType != typeof(BinStreamExtensions))
        throw new InvalidOperationException("Method must be declared at BinStreamExtensions class only");

      var parameters = method.GetParameters();
      if (parameters.Length != 1)
        throw new ArgumentException("Reader has invalid parameters count. Method must have 2 parameters.");

      if (parameters[0].ParameterType != typeof(Stream))
        throw new ArgumentException("Reader has invalid parameters. First parameter must be Stream.");

      if (method.ReturnType == typeof(void))
        throw new ArgumentException("Reader has invalid return type. Method must return anything.");
    }

    private static void ValidateTypeReader(MethodInfo method)
    {
      if (!method.IsStatic)
        throw new ArgumentException("Reader method must be static.");

      var parameters = method.GetParameters();
      if (parameters.Length != 3)
        throw new ArgumentException("Reader has invalid parameters count. Method must have 2 parameters.");

      if (parameters[0].ParameterType != typeof(Stream))
        throw new ArgumentException("Reader has invalid parameters. First parameter must be Stream.");

      if (parameters[1].ParameterType != method.ReturnType)
        throw new ArgumentException("Reader has invalid parameters. Second parameter type must be equals to return type.");

      if (parameters[2].ParameterType != typeof(int))
        throw new ArgumentException("Reader has invalid parameters. Third parameter type must be int.");

      if (method.ReturnType == typeof(void))
        throw new ArgumentException("Reader has invalid return type. Method must return anything.");
    }

    private static void ValidateStreamSkiper(MethodInfo method)
    {
      if (method.DeclaringType != typeof(BinStreamExtensions))
        throw new InvalidOperationException("Method must be declared at BinStreamExtensions class only");

      ValidateTypeSkiper(method);
    }

    private static void ValidateTypeSkiper(MethodInfo method)
    {
      if (!method.IsStatic)
        throw new ArgumentException("Skiper method must be static.");

      var parameters = method.GetParameters();
      if (parameters.Length != 1)
        throw new ArgumentException("Skiper has invalid parameters count. Method must have 1 parameters.");

      if (parameters[0].ParameterType != typeof(Stream))
        throw new ArgumentException("Skiper has invalid parameters. First parameter must be Stream.");

      if (method.ReturnType != typeof(void))
        throw new ArgumentException("Skiper has invalid return type. Method must return nothing.");
    }

    internal bool IsValid(TypeInfo type)
    {
      if (TypeWriter != null && !IsGenericArgsValid(TypeWriter, type))
        return false;

      if (TypeReader != null && !IsGenericArgsValid(TypeReader, type))
        return false;

      return true;
    }

    private static bool IsGenericArgsValid(MethodInfo method, TypeInfo type)
    {
      var methodGenericParameters = method.GetGenericArguments();
      var paramGenericParameters = type.GetGenericArguments();
      return methodGenericParameters.Length == paramGenericParameters.Length;
    }
  }
}
