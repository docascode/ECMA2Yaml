using ECMA2Yaml.Models;
using Monodoc.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public static class IdExtensions
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

        public static string ToSpecId(this string typeStr, List<string> knownTypeParamsOnType = null, List<string> knownTypeParamsOnMember = null)
        {
            if (!NeedParseByECMADesc(typeStr))
            {
                return typeStr;
            }
            return ECMAStore.GetOrAddTypeDescriptor(typeStr).ToSpecId(knownTypeParamsOnType, knownTypeParamsOnMember) ?? typeStr;
        }

        public static string ToSpecId(this EcmaDesc desc, List<string> knownTypeParamsOnType = null, List<string> knownTypeParamsOnMember = null)
        {
            if (desc == null)
            {
                return null;
            }
            var typeStr = string.IsNullOrEmpty(desc.Namespace) ? desc.TypeName : desc.Namespace + "." + desc.TypeName;
            if (desc.GenericTypeArgumentsCount > 0)
            {
                var typeparameterPart = string.Join(",", desc.GenericTypeArguments.Select(ta =>
                {
                    var i = knownTypeParamsOnType?.IndexOf(ta.TypeName);
                    if (i.HasValue && i.Value >= 0)
                    {
                        return $"`{i.Value}";
                    }
                    i = knownTypeParamsOnMember?.IndexOf(ta.TypeName);
                    if (i.HasValue && i.Value >= 0)
                    {
                        return $"``{i.Value}";
                    }
                    return ta.ToSpecId(knownTypeParamsOnType, knownTypeParamsOnMember);
                }));
                typeStr = string.Format("{0}{{{1}}}", typeStr, typeparameterPart);
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
            if (desc.NestedType != null)
            {
                typeStr += ("." + desc.NestedType.ToSpecId(knownTypeParamsOnType, knownTypeParamsOnMember));
            }
            return typeStr;
        }

        public static string ToOuterTypeUid(this string typeStr)
        {
            if (!NeedParseByECMADesc(typeStr))
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
            if (store.ItemsByDocId.TryGetValue(commentId, out var item))
            {
                return item;
            }
            var parts = commentId.Split(':');
            if (parts?.Length != 2)
            {
                OPSLogger.LogUserError("Unable to parse string as comment id: " + commentId);
                return null;
            }
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

        private static bool NeedParseByECMADesc(string typeStr)
        {
            return (!string.IsNullOrEmpty(typeStr) && (typeStr.Contains('<') || typeStr.Contains('+')));
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
