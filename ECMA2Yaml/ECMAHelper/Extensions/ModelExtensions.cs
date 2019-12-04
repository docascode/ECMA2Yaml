using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ECMA2Yaml
{
    public static class ModelExtensions
    {
        public static T[] GetAllPublicConstantValues<T>(this Type type)
        {
            return type
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(T))
                .Select(x => (T)x.GetRawConstantValue())
                .ToArray();
        }
    }
}
