﻿using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using Monodoc.Ecma;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        public static string TypeStringToTypeMDString(string typeStr, ECMAStore store)
        {
            if (store.TryGetTypeByFullName(typeStr, out var t))
            {
                return EncodeXrefLink(t.Name, t.Uid);
            }

            var desc = ECMAStore.GetOrAddTypeDescriptor(typeStr);
            if (desc != null)
            {
                return DescToTypeMDString(desc);
            }
            return typeStr;
        }

        public static string DocIdToTypeMDString(string docId, ECMAStore store)
        {
            var item = docId.ResolveCommentId(store);
            if (item != null)
            {
                if (item is Member m)
                {
                    return EncodeXrefLink(m.DisplayName, item.Uid);
                }
                else
                {
                    return EncodeXrefLink(item.Name, item.Uid);
                }
            }
            var (_, uid) = docId.ParseCommentId();
            return UidToTypeMDString(uid, store);
        }

        public static string UidToTypeMDString(string uid, ECMAStore store)
        {
            if (store.TypesByUid.TryGetValue(uid, out var t))
            {
                return EncodeXrefLink(t.Name, t.Uid);
            }
            if (store.MembersByUid.TryGetValue(uid, out var m))
            {
                return EncodeXrefLink(m.Name, m.Uid);
            }
            return $"<xref href=\"{uid}\" data-throw-if-not-resolved=\"True\"/>";
        }

        private static readonly string[] ArrayDimensionSuffix = new string[] { "", "[]", "[,]", "[,,]", "[,,,]", "[,,,,]", "[,,,,,]" };
        public static string DescToTypeMDString(EcmaDesc desc, string parentTypeUid = null, string parentName = null)
        {
            var typeUid = string.IsNullOrEmpty(parentTypeUid) ? desc.ToOuterTypeUid() : (parentTypeUid + "." + desc.ToOuterTypeUid());
            var typeName = string.IsNullOrEmpty(parentName) ? desc.TypeName : (parentName + "." + desc.TypeName);

            if (desc.NestedType != null && desc.GenericTypeArgumentsCount == 0)
            {
                return DescToTypeMDString(desc.NestedType, typeUid, typeName);
            }

            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrEmpty(parentTypeUid) && IsTypeArgument(desc))
            {
                sb.Append(typeName);
            }
            else if (desc.GenericTypeArgumentsCount > 0)
            {
                var altText = string.IsNullOrEmpty(desc.Namespace)
                 ? typeName : $"{desc.Namespace}.{typeName}";
                sb.Append(EncodeXrefLink(typeName, typeUid, altText));
            }
            else
            {
                sb.Append(EncodeXrefLink(typeName, typeUid));
            }

            if (desc.GenericTypeArgumentsCount > 0)
            {
                sb.Append($"&lt;{HandleTypeArgument(desc.GenericTypeArguments.First())}");
                for (int i = 1; i < desc.GenericTypeArgumentsCount; i++)
                {
                    sb.Append($",{HandleTypeArgument(desc.GenericTypeArguments[i])}");
                }
                sb.Append("&gt;");
            }

            if (desc.NestedType != null)
            {
                sb.Append($".{DescToTypeMDString(desc.NestedType, typeUid)}");
            }

            if (desc.ArrayDimensions != null && desc.ArrayDimensions.Count > 0)
            {
                foreach (var arr in desc.ArrayDimensions)
                {
                    sb.Append(ArrayDimensionSuffix[arr]);
                }
            }
            if (desc.DescModifier == EcmaDesc.Mod.Pointer)
            {
                sb.Append("*");
            }

            return sb.ToString();

            string HandleTypeArgument(EcmaDesc d)
            {
                if (IsTypeArgument(d) && d.ArrayDimensions == null)
                {
                    return d.TypeName;
                }
                return DescToTypeMDString(d);
            }

            bool IsTypeArgument(EcmaDesc d)
            {
                return (string.IsNullOrEmpty(d.Namespace) && d.DescKind == EcmaDesc.Kind.Type && d.NestedType == null);
            }
        }

        public static string EncodeXrefLink(string text, string uid, string altText = null)
        {
            return $"<xref href=\"{uid}?alt={altText ?? uid}&text={UrlEncodeLinkText(text)}\" data-throw-if-not-resolved=\"True\"/>";
        }

        public static TypeMemberLink ConvertTypeMemberLink(Models.Type t, Member m)
        {
            if (m == null)
            {
                return null;
            }
            var monikers = m.Monikers;
            VersionedString inheritanceInfo = null;
            if (t?.InheritedMembers != null
                && t.InheritedMembers.TryGetValue(m.Uid, out inheritanceInfo)
                && inheritanceInfo.Monikers != null)
            {
                monikers = inheritanceInfo.Monikers;
            }
            if (monikers.Any())
            {
                return new TypeMemberLink()
                {
                    Uid = m.Uid,
                    InheritedFrom = inheritanceInfo != null ? m.Parent.Uid : null,
                    Monikers = monikers
                };
            }
            return null;
        }

        public TypeMemberLink ExtensionMethodToTypeMemberLink(Models.Type t, VersionedString vs)
        {
            HashSet<string> monikers = vs.Monikers;
            if (monikers == null && _store.MembersByUid.TryGetValue(vs.Value, out var m))
            {
                monikers = m.Monikers;
            }
            if (monikers != null)
            {
                if (monikers.Count > t.Monikers.Count)
                {
                    monikers = monikers.Intersect(t.Monikers).ToHashSet();
                }
                if (monikers.SetEquals(t.Monikers))
                {
                    monikers = null;
                }
            }
            return new TypeMemberLink()
            {
                Uid = vs.Value,
                Monikers = monikers
            };
        }

        public static NamespaceTypeLink ConvertNamespaceTypeLink(Namespace ns, Models.Type t)
        {
            if (t == null)
            {
                return null;
            }
            return new NamespaceTypeLink()
            {
                Uid = t.Uid,
                Monikers = t.Monikers
            };
        }

        public static IEnumerable<VersionedString> MonikerizeAssemblyStrings(ReflectionItem item)
        {
            if (item.VersionedAssemblyInfo == null)
            {
                //legacy xml, fallback to asseblies without versions
                return item.AssemblyInfo?.Select(asm => new VersionedString() { Value = asm.Name + ".dll" }).ToList();
            }
            var monikerAssembliesPairs = item.VersionedAssemblyInfo.ValuesPerMoniker
                .Select(pair => (
                moniker: pair.Key,
                asmStr: string.Join(", ", pair.Value.OrderBy(asm => asm.Name).Select(asm => asm.Name + ".dll"))
                ))
                .ToList();
            var versionedList =  monikerAssembliesPairs
                .GroupBy(p => p.asmStr)
                .Select(g => new VersionedString() { Value = g.Key, Monikers = g.Select(p => p.moniker).ToHashSet() })
                .ToList();
            if (versionedList.Count == 1)
            {
                versionedList.First().Monikers = null;
            }
            return versionedList;
        }

        private static string HtmlEncodeLinkText(string text)
        {
            return WebUtility.HtmlEncode(text);
        }

        private static string UrlEncodeLinkText(string text)
        {
            return WebUtility.UrlEncode(text);
        }
    }
}
