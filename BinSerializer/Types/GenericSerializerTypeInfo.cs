using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.Text;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  sealed class GenericSerializerTypeInfo : SerializerTypeInfo
  {
    private struct TypeGenericArguments : IEquatable<TypeGenericArguments>
    {
      private readonly Type[] _types;

      public TypeGenericArguments(Type[] types)
      {
        _types = types;
      }

      public override bool Equals(object obj)
      {
        if (ReferenceEquals(obj, null))
          return false;
        if (obj.GetType() != GetType())
          return false;

        var other = (TypeGenericArguments)obj;
        return Equals(other);
      }

      public bool Equals(TypeGenericArguments other)
      {
        if (_types.Length != other._types.Length)
          return false;

        for (int i = 0; i < _types.Length; i++)
        {
          var t = _types[i];
          var o = other._types[i];
          if (t != o)
            return false;
        }

        return true;
      }

      public override int GetHashCode()
      {
        int hashCode = 0;
        for (int i = 0; i < _types.Length; i++)
          hashCode = (hashCode * 397) ^ _types[i].GetHashCode();
        return hashCode;
      }
    }

    private readonly ConcurrentDictionary<TypeGenericArguments, MethodInfo> _builtWriters;
    private readonly ConcurrentDictionary<TypeGenericArguments, MethodInfo> _builtReaders;
    
    [SecurityCritical]
    public GenericSerializerTypeInfo(BinTypeDescription description, BinTypeVersion version, BinTypeProcess process) 
      : base(description, version, process)
    {
      if (!Type.IsGenericType)
        throw new ArgumentException("Type must be generic.");

      if (!Type.IsGenericTypeDefinition)
        throw new ArgumentException("Generic type must be opened.");

      if (IsGenericTypeId(TypeId))
        throw new ArgumentException("Type id must be declared as non generic");

      _builtWriters = new ConcurrentDictionary<TypeGenericArguments, MethodInfo>();
      _builtReaders = new ConcurrentDictionary<TypeGenericArguments, MethodInfo>();
    }

    public override MethodInfo GetWriter(Type notNormalizedType)
    {
      var writer = base.GetWriter(notNormalizedType);
      if (writer == null)
        return null;

      if (writer.IsGenericMethodDefinition)
      {
        var genericArgs = notNormalizedType.GetGenericArguments();
        var key = new TypeGenericArguments(genericArgs);

        MethodInfo cachedWriter;
        if (_builtWriters.TryGetValue(key, out cachedWriter))
          return cachedWriter;

        var parameters = writer.GetParameters();
        var methodGenericArgs = BuildMethodGenericArguments(writer, parameters[1].ParameterType, genericArgs);
        var builtMethod = writer.MakeGenericMethod(methodGenericArgs);

        _builtWriters.TryAdd(key, builtMethod);
        return builtMethod;
      }
      return writer;
    }

    public override MethodInfo GetReader(Type notNormalizedType)
    {
      var reader = base.GetReader(notNormalizedType);
      if (reader == null)
        return null;

      if (reader.IsGenericMethodDefinition)
      {
        var genericArgs = notNormalizedType.GetGenericArguments();
        var key = new TypeGenericArguments(genericArgs);

        MethodInfo cachedWriter;
        if (_builtReaders.TryGetValue(key, out cachedWriter))
          return cachedWriter;

        var methodGenericArgs = BuildMethodGenericArguments(reader, reader.ReturnType, genericArgs);
        var builtMethod = reader.MakeGenericMethod(methodGenericArgs);

        _builtReaders.TryAdd(key, builtMethod);
        return builtMethod;
      }
      return reader;
    }

    private Type[] BuildMethodGenericArguments(MethodInfo method, Type type, Type[] typeClosedGenericArgs)
    {
      var methodGenericArgs = method.GetGenericArguments();
      var parameterGenericArgs = type.GetGenericArguments();

      var result = new Type[methodGenericArgs.Length];
      for (int i = 0; i < result.Length; i++)
      {
        var genericArgsIndex = Array.IndexOf(parameterGenericArgs, methodGenericArgs[i]);
        result[i] = typeClosedGenericArgs[genericArgsIndex];
      }
      return result;
    }

    // Must be called read under SerializerTypes read lock
    [SecuritySafeCritical]
    public override Type GetType(string notNormalizedTypeId)
    {
      var index = 0;
      var types = new Type[Type.GenericTypeParameters.Length];

      foreach (var genericArgumentId in EnumerateGenericTypeIds(notNormalizedTypeId))
        types[index++] = SerializerTypes.GetTypeImpl(genericArgumentId);

      return Type.MakeGenericType(types);
    }

    // Must be called read under SerializerTypes read lock
    [SecuritySafeCritical]
    public override string GetTypeId(Type notNormalizedType)
    {
      if (notNormalizedType.ContainsGenericParameters)
        throw new ArgumentException(string.Format("{0} conatins generic parameters.", Type));

      var builder = new StringBuilder();
      builder.Append(TypeId);
      builder.Append('[');

      var argumentsCount = notNormalizedType.GenericTypeArguments.Length;
      for (int i = 0; i < argumentsCount; i++)
      {
        var genericTypeArgument = notNormalizedType.GenericTypeArguments[i];
        var typeId = SerializerTypes.GetTypeIdImpl(genericTypeArgument);

        builder.Append(typeId);

        var isLast = i == argumentsCount - 1;
        if (!isLast)
          builder.Append(",");
      }

      builder.Append("]");

      return builder.ToString();
    }

    [SecurityCritical]
    private static bool IsGenericTypeId(string typeId)
    {
      var index1 = typeId.IndexOf('[');
      if (index1 < 0)
        return false;

      var index2 = typeId.LastIndexOf(']');
      if (index2 < 0)
        return false;

      return index2 > index1;
    }

    [SecurityCritical]
    private static IEnumerable<string> EnumerateGenericTypeIds(string genericTypeId)
    {
      var index1 = genericTypeId.IndexOf('[');
      var index2 = genericTypeId.LastIndexOf(']');

      if (index1 < 0 || index2 < 0)
        throw new ArgumentException("Is not a generic typeId");

      var builder = new StringBuilder();
      var bracketCounter = 1;

      for (int i = index1 + 1; i <= index2; i++)
      {
        var ch = genericTypeId[i];

        if (ch == '[')
          bracketCounter++;
        if (ch == ']')
          bracketCounter--;

        if (bracketCounter == 0)
        {
          yield return builder.ToString();
        }
        else
        {
          if (ch != ',' || bracketCounter != 1)
            builder.Append(ch);
          else
          {
            yield return builder.ToString();
            builder.Clear();
          }
        }
      }
    }
  }
}
