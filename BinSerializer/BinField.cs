using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
    internal struct BinField
    {
        private readonly BinFieldAttribute _attribute;
        private readonly FieldInfo _field;
        private readonly MethodInfo _getMethod;
        private readonly MethodInfo _setMethod;

        public BinField(BinFieldAttribute attribute, FieldInfo fieldInfo)
        {
            _attribute = attribute;
            _field = fieldInfo;
            _getMethod = null;
            _setMethod = null;
        }

        public BinField(BinFieldAttribute attribute, MethodInfo getMethod, MethodInfo setMethod)
        {
            _attribute = attribute;
            _field = null;
            _getMethod = getMethod;
            _setMethod = setMethod;
        }

        public string Id
        {
            [SecurityCritical]
            get { return _attribute.Id; }
        }

        public bool IsField
        {
            [SecurityCritical]
            get { return _field != null; }
        }

        public Type Type
        {
            [SecurityCritical]
            get
            {
                return IsField
                  ? _field.FieldType
                  : _getMethod.ReturnType;
            }
        }

        public bool IsValueType
        {
            [SecurityCritical]
            get { return Type.IsValueType; }
        }

        [SecurityCritical]
        public void SetValue(object instance, object value)
        {
            if (IsField)
                _field.SetValue(instance, value);
            else
                _setMethod.Invoke(instance, new[] { value });
        }

        [SecurityCritical]
        public void EmitRead(ILGenerator il)
        {
            if (IsField)
                il.Emit(OpCodes.Ldfld, _field);
            else
                il.Emit(OpCodes.Callvirt, _getMethod);
        }

        [SecurityCritical]
        public void EmitWrite(ILGenerator il)
        {
            if (IsField)
                il.Emit(OpCodes.Stfld, _field);
            else
                il.Emit(OpCodes.Callvirt, _setMethod);
        }

        [SecurityCritical]
        public static IEnumerable<BinField> Get(Type type)
        {
            return GetFields(type).Concat(GetProperties(type));
        }

        [SecurityCritical]
        private static IEnumerable<BinField> GetFields(Type type)
        {
            var fields = type
              .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
              .Select(f => new BinField(f.GetCustomAttribute<BinFieldAttribute>(false), f))
              .Where(f => f._attribute != null);

            Type currentType = type;
            while (true)
            {
                currentType = currentType.BaseType;
                if (currentType == typeof(object))
                    break;

                fields = currentType
                  .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                  .Where(f => !f.IsFamily)
                  .Select(f => new BinField(f.GetCustomAttribute<BinFieldAttribute>(false), f))
                  .Where(f => f._attribute != null)
                  .Concat(fields);
            }

            var declaredIds = new HashSet<string>();
            foreach (var binField in fields.OrderBy(p => p._attribute.Id))
            {
                if (!declaredIds.Add(binField._attribute.Id))
                    throw new ArgumentException(string.Format("Field \"{0}\" declared twice in {1} type", binField._attribute.Id, binField._field.DeclaringType));
                if (binField._field.IsInitOnly)
                    throw new ArgumentException(string.Format("Field {0} can't be readonly (IsInitOnly = true). For type {1}", binField._field.Name, binField._field.DeclaringType));
                yield return binField;
            }
        }

        private struct PropertyKey : IEquatable<PropertyKey>
        {
            public readonly string Name;
            public readonly Type DeclaringType;

            public PropertyKey(string name, Type declaringType)
            {
                Name = name;
                DeclaringType = declaringType;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(obj, null))
                    return false;
                if (obj is PropertyKey key)
                    return Equals(key);
                return false;
            }

            public bool Equals(PropertyKey other)
            {
                return string.Equals(other.Name, Name, StringComparison.Ordinal) && other.DeclaringType == DeclaringType;
            }

            public override int GetHashCode()
            {
                return (Name.GetHashCode() * 397) ^ DeclaringType.GetHashCode();
            }
        }

        private class Property
        {
            public readonly PropertyKey Key;
            public readonly BinFieldAttribute Attribute;

            public MethodInfo GetMethod;
            public MethodInfo SetMethod;

            public Property(PropertyKey key, BinFieldAttribute attribute)
            {
                Key = key;
                Attribute = attribute;
            }
        }

        [SecurityCritical]
        private static IEnumerable<BinField> GetProperties(Type type)
        {
            var properties = new Dictionary<PropertyKey, Property>();

            Type currentType = type;
            while (true)
            {
                var pairs = currentType
                  .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                  .Select(p => new { Property = p, Attribute = p.GetCustomAttribute<BinFieldAttribute>(false) })
                  .Where(p => p.Attribute != null);

                foreach (var pair in pairs)
                {
                    var key = new PropertyKey(pair.Property.Name, pair.Property.DeclaringType);
                    var getMethod = pair.Property.GetGetMethod(true);
                    var setMethod = pair.Property.GetSetMethod(true);

                    Property property;
                    if (!properties.TryGetValue(key, out property))
                        properties.Add(key, property = new Property(key, pair.Attribute));

                    if (property.GetMethod == null)
                        property.GetMethod = getMethod;

                    if (property.SetMethod == null)
                        property.SetMethod = setMethod;
                }

                currentType = currentType.BaseType;
                if (currentType == typeof(object))
                    break;
            }

            var declaredIds = new HashSet<string>();
            foreach (var property in properties.Values.OrderBy(p => p.Attribute.Id))
            {
                if (!declaredIds.Add(property.Attribute.Id))
                    throw new ArgumentException(string.Format("Field \"{0}\" declared twice in {1} type", property.Attribute.Id, property.Key.DeclaringType));
                if (property.SetMethod == null)
                    throw new ArgumentException(string.Format("Property {0} should have set method. For type {1}", property.Key.Name, property.Key.DeclaringType));
                if (property.GetMethod == null)
                    throw new ArgumentException(string.Format("Property {0} should have get method. For type {1}", property.Key.Name, property.Key.DeclaringType));

                yield return new BinField(property.Attribute, property.GetMethod, property.SetMethod);
            }
        }
    }
}