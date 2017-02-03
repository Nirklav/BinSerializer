using System;
using System.IO;
using System.Reflection;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  public sealed class BinTypeProcess
  {
    public readonly MethodInfo Writer;
    public readonly MethodInfo Reader;
    public readonly MethodInfo Skiper;

    public BinTypeProcess(MethodInfo writer, MethodInfo reader, MethodInfo skiper)
    {
      if (writer != null)
        ValidateWriter(writer);
      if (reader != null)
        ValidateReader(reader);
      if (skiper != null)
        ValidateSkiper(skiper);

      Writer = writer;
      Reader = reader;
      Skiper = skiper;
    }

    private static void ValidateWriter(MethodInfo method)
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

      if (!IsGenericArgsValid(method, parameters[1].ParameterType))
        throw new ArgumentException("Generic arguments not valid.");
    }

    private static void ValidateReader(MethodInfo method)
    {
      if (!method.IsStatic)
        throw new ArgumentException("Reader method must be static.");

      var parameters = method.GetParameters();
      if (parameters.Length != 1)
        throw new ArgumentException("Reader has invalid parameters count. Method must have 1 parameters.");

      if (parameters[0].ParameterType != typeof(Stream))
        throw new ArgumentException("Reader has invalid parameters. First parameter must be Stream.");

      if (method.ReturnType == typeof(void))
        throw new ArgumentException("Reader has invalid return type. Method must return anything.");

      if (!IsGenericArgsValid(method, method.ReturnType))
        throw new ArgumentException("Generic arguments not valid.");
    }

    private static void ValidateSkiper(MethodInfo method)
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

      if (method.IsGenericMethod)
        throw new ArgumentException("Skiper not suppots generics.");
    }

    private static bool IsGenericArgsValid(MethodInfo method, Type type)
    {
      var methoodGenericParameters = method.GetGenericArguments();
      var paramGenericParameters = type.GetGenericArguments();
      return paramGenericParameters.Length >= methoodGenericParameters.Length;
    }
  }
}
