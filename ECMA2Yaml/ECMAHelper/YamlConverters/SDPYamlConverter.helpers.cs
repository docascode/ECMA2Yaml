using ECMA2Yaml.Models;
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
            return docId;
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
            return $"<xref:{uid}>";
        }

        public static string DescToTypeMDString(EcmaDesc desc, string parentTypeUid = null)
        {
            var typeUid = string.IsNullOrEmpty(parentTypeUid) ? desc.ToOuterTypeUid() : (parentTypeUid + "." + desc.ToOuterTypeUid());
            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrEmpty(parentTypeUid) && IsTypeArgument(desc))
            {
                sb.Append(desc.TypeName);
            }
            else if (desc.GenericTypeArgumentsCount > 0)
            {
                var altText = string.IsNullOrEmpty(desc.Namespace)
                 ? desc.TypeName : $"{desc.Namespace}.{desc.TypeName}";
                sb.Append(EncodeXrefLink(desc.TypeName, typeUid, altText));
            }
            else
            {
                sb.Append(EncodeXrefLink(desc.TypeName, typeUid));
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
                    sb.Append("[]");
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
                return (string.IsNullOrEmpty(d.Namespace) && d.DescKind == EcmaDesc.Kind.Type);
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
            var rval = new TypeMemberLink()
            {
                Uid = m.Uid,
                InheritedFrom = (t != null && m.Parent.Uid != t.Uid) ? m.Parent.Uid : null
            };
            if (m.Metadata.TryGetValue(OPSMetadata.Monikers, out var monikers))
            {
                rval.Monikers = (IEnumerable<string>)monikers;
            }
            return rval;
        }

        public static NamespaceTypeLink ConvertNamespaceTypeLink(Namespace ns, Models.Type t)
        {
            if (t == null)
            {
                return null;
            }
            var rval = new NamespaceTypeLink()
            {
                Uid = t.Uid
            };
            if (t.Metadata.TryGetValue(OPSMetadata.Monikers, out var monikers))
            {
                rval.Monikers = (IEnumerable<string>)monikers;
            }
            return rval;
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
