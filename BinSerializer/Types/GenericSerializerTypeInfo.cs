using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace ThirtyNineEighty.BinarySerializer.Types
{
  sealed class GenericSerializerTypeInfo : SerializerTypeInfo
  {
    [SecurityCritical]
    public GenericSerializerTypeInfo(BinTypeDescription description, BinTypeVersion version, BinTypeProcess process) 
      : base(description, version, process)
    {
      if (!_type.IsGenericType)
        throw new ArgumentException("Type must be generic.");

      if (!_type.IsGenericTypeDefinition)
        throw new ArgumentException("Generic type must be opened.");

      if (IsGenericTypeId(_typeId))
        throw new ArgumentException("Type id must be declared as non generic");
    }

    // Must be called read under SerializerTypes read lock
    [SecuritySafeCritical]
    public override Type GetType(string notNormalizedTypeId)
    {
      var index = 0;
      var types = new Type[_type.GenericTypeParameters.Length];

      foreach (var genericArgumentId in EnumerateGenericTypeIds(notNormalizedTypeId))
        types[index++] = SerializerTypes.GetTypeImpl(genericArgumentId);

      return _type.MakeGenericType(types);
    }

    // Must be called read under SerializerTypes read lock
    [SecuritySafeCritical]
    public override string GetTypeId(Type notNormalizedType)
    {
      if (notNormalizedType.ContainsGenericParameters)
        throw new ArgumentException(string.Format("{0} conatins generic parameters.", _type));

      var builder = new StringBuilder();
      builder.Append(_typeId);
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
