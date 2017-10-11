using ECMA2Yaml.Models;
using Monodoc.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public static class EcmaDescExtensions
    {
        public static string ToDisplayName(this string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr))
            {
                return typeStr;
            }
            if (!typeStr.Contains('<'))
            {
                var parts = typeStr.Split('.');
                return parts.Last();
            }

            return ECMAStore.GetOrAddTypeDescriptor(typeStr).ToDisplayName();
        }

        public static string ToSpecId(this string typeStr, List<string> knownTypeParams = null)
        {
            if (string.IsNullOrEmpty(typeStr) || !typeStr.Contains('<'))
            {
                return typeStr;
            }
            return ECMAStore.GetOrAddTypeDescriptor(typeStr).ToSpecId(knownTypeParams);
        }

        public static string ToSpecId(this EcmaDesc desc, List<string> knownTypeParams = null)
        {
            if (desc == null)
            {
                return null;
            }
            var typeStr = string.IsNullOrEmpty(desc.Namespace) ? desc.TypeName : desc.Namespace + "." + desc.TypeName;
            if (desc.GenericTypeArgumentsCount > 0)
            {
                if (knownTypeParams != null && desc.GenericTypeArguments.All(ta => knownTypeParams.Contains(ta.TypeName)))
                {
                    typeStr = string.Format("{0}`{1}", typeStr, desc.GenericTypeArgumentsCount);
                }
                else
                {
                    typeStr = string.Format("{0}{{{1}}}", typeStr, string.Join(",", desc.GenericTypeArguments.Select(d => d.ToSpecId(knownTypeParams))));
                }
            }
            if (desc.ArrayDimensions?.Count > 0)
            {
                for (int i = 0; i < desc.ArrayDimensions?.Count; i++)
                {
                    typeStr = typeStr + "[]";
                }
            }
            if (desc.DescModifier == EcmaDesc.Mod.Pointer)
            {
                typeStr += "*";
            }
            return typeStr;
        }

        public static string ToOuterTypeUid(this string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || !typeStr.Contains('<'))
            {
                return typeStr;
            }
            return ECMAStore.GetOrAddTypeDescriptor(typeStr).ToOuterTypeUid();
        }

        public static string ToOuterTypeUid(this EcmaDesc desc)
        {
            if (desc == null)
            {
                return null;
            }
            var typeStr = string.IsNullOrEmpty(desc.Namespace) ? desc.TypeName : (desc.Namespace + "." + desc.TypeName);
            if (desc.GenericTypeArgumentsCount > 0)
            {
                typeStr += "`" + desc.GenericTypeArgumentsCount;
            }

            return typeStr;
        }

        public static string ToDisplayName(this EcmaDesc desc)
        {
            if (desc == null)
            {
                return null;
            }

            string name = null;
            if (desc.GenericTypeArgumentsCount == 0)
            {
                name = desc.TypeName;
            }
            else
            {
                name = string.Format("{0}<{1}>", desc.TypeName, string.Join(",", desc.GenericTypeArguments.Select(d => d.ToDisplayName())));
            }
            if (desc.ArrayDimensions?.Count > 0)
            {
                for (int i = 0; i < desc.ArrayDimensions?.Count; i++)
                {
                    name += "[]";
                }
            }
            if (desc.DescModifier == EcmaDesc.Mod.Pointer)
            {
                name += "*";
            }

            return name;
        }

        public static ReflectionItem ResolveCommentId(this string commentId, ECMAStore store)
        {
            if (string.IsNullOrEmpty(commentId))
            {
                return null;
            }
            var parts = commentId.Split(':');
            switch (parts[0])
            {
                case "N":
                    return store.Namespaces.ContainsKey(parts[1]) ? store.Namespaces[parts[1]] : null;
                case "T":
                    return store.TypesByUid.ContainsKey(parts[1]) ? store.TypesByUid[parts[1]] : null;
                default:
                    return store.MembersByUid.ContainsKey(parts[1]) ? store.MembersByUid[parts[1]] : null;
            }
        }

        public static void AddWithKey(this Dictionary<string, List<string>> dict, string key, string val)
        {
            if (!dict.ContainsKey(key))
            {
                dict[key] = new List<string>();
            }
            dict[key].Add(val);
        }

        public static List<string> GetOrDefault(this Dictionary<string, List<string>> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return null;
        }

        public static IEnumerable<T> NullIfEmpty<T>(this IEnumerable<T> list)
        {
            if (list == null || !list.Any())
            {
                return null;
            }
            return list;
        }
    }

    public class TypeIdComparer : IComparer<string>
    {

        public int Compare(string stringA, string stringB)
        {
            String[] valueA = stringA.Split('`');
            String[] valueB = stringB.Split('`');

            if (valueA.Length != 2 || valueB.Length != 2)
                return String.Compare(stringA, stringB);

            int iA = 0, iB = 0;
            if (valueA[0] == valueB[0] && int.TryParse(valueA[1], out iA) && int.TryParse(valueB[1], out iB))
            {
                return iA.CompareTo(iB);
            }
            else
            {
                return String.Compare(valueA[0], valueB[0]);
            }

        }

    }
}
