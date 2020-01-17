using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        private readonly ECMAStore _store;
        private readonly bool _withVersioning;

        private static string[] defaultLangList = new string[] { "csharp" };

        public Dictionary<string, ItemSDPModelBase> NamespacePages { get; } = new Dictionary<string, ItemSDPModelBase>();
        public Dictionary<string, ItemSDPModelBase> TypePages { get; } = new Dictionary<string, ItemSDPModelBase>();
        public Dictionary<string, ItemSDPModelBase> MemberPages { get; } = new Dictionary<string, ItemSDPModelBase>();
        public Dictionary<string, ItemSDPModelBase> OverloadPages { get; } = new Dictionary<string, ItemSDPModelBase>();

        public SDPYamlConverter(ECMAStore store, bool withVersioning = false)
        {
            _store = store;
            _withVersioning = withVersioning;
        }

        public void Convert()
        {
            HashSet<string> memberTouchCache = new HashSet<string>();

            foreach (var ns in _store.Namespaces)
            {
                NamespacePages.Add(ns.Key, FormatNamespace(ns.Value));
            }

            foreach (var type in _store.TypesByUid.Values)
            {
                switch (type.ItemType)
                {
                    case ItemType.Enum:
                        var enumPage = FormatEnum(type, memberTouchCache);
                        TypePages.Add(enumPage.Uid, enumPage);
                        break;
                    case ItemType.Class:
                    case ItemType.Interface:
                    case ItemType.Struct:
                        var tPage = FormatType(type);
                        TypePages.Add(tPage.Uid, tPage);
                        break;
                    case ItemType.Delegate:
                        var dPage = FormatDelegate(type);
                        TypePages.Add(dPage.Uid, dPage);
                        break;
                }

                var mGroups = type.Members
                    ?.Where(m => !memberTouchCache.Contains(m.Uid))
                    .GroupBy(m => m.Overload);
                if (mGroups != null)
                {
                    foreach (var mGroup in mGroups)
                    {
                        var parentType = (Models.Type)mGroup.FirstOrDefault()?.Parent;
                        var ol = parentType?.Overloads.FirstOrDefault(o => o.Uid == mGroup.Key);
                        if (mGroup.Key == null)
                        {
                            foreach (var m in mGroup)
                            {
                                OverloadPages.Add(m.Uid, FormatOverload(null, new List<Member> { m }));
                            }
                        }
                        else
                        {
                            OverloadPages.Add(mGroup.Key, FormatOverload(ol, mGroup.ToList()));
                        }
                    }
                }
            }
        }

        private T InitWithBasicProperties<T>(ReflectionItem item) where T : ItemSDPModelBase, new()
        {
            T rval = new T
            {
                Uid = item.Uid,
                CommentId = item.CommentId,
                Name = item.Name,

                Assemblies = item.AssemblyInfo?.Select(asm => asm.Name).Distinct().ToList(),
                DevLangs = item.Signatures?.DevLangs ?? defaultLangList,

                SeeAlso = BuildSeeAlsoList(item.Docs, _store),
                Summary = item.Docs.Summary,
                Remarks = item.Docs.Remarks,
                Examples = item.Docs.Examples,
                Monikers = item.Monikers,
                Source = item.SourceDetail.ToSDPSourceDetail()
            };

            if(_withVersioning)
            {
                rval.AttributesWithMoniker = item.Attributes?.Where(att => att.Visible)
                    .Select(att => new VersionedString() { Value = att.TypeFullName, Monikers = att.Monikers?.ToHashSet() })
                    .ToList().NullIfEmpty();
                rval.AttributeMonikers = ConverterHelper.ConsolidateVersionedValues(rval.AttributesWithMoniker, item.Monikers);
                rval.SyntaxWithMoniker = ConverterHelper.BuildVersionedSignatures(item)?.NullIfEmpty();
            }
            else
            {
                rval.Attributes = item.Attributes?.Where(att => att.Visible).Select(att => att.TypeFullName)
                    .ToList().NullIfEmpty();
                var rawSignatures = _store.UWPMode ? ConverterHelper.BuildUWPSignatures(item) : ConverterHelper.BuildSignatures(item);
                rval.Syntax = rawSignatures?.Select(sig => new SignatureModel() { Lang = sig.Key, Value = sig.Value }).ToList();
            }

            switch (item)
            {
                case Member m:
                    rval.Namespace = string.IsNullOrEmpty(m.Parent.Parent.Name) ? null : m.Parent.Parent.Name;
                    rval.FullName = m.FullDisplayName;
                    rval.Name = m.DisplayName;
                    rval.NameWithType = m.Parent.Name + '.' + m.DisplayName;
                    break;
                case ECMA2Yaml.Models.Type t:
                    rval.Namespace = string.IsNullOrEmpty(t.Parent.Name) ? null : t.Parent.Name;
                    rval.FullName = t.FullName;
                    rval.NameWithType = t.FullName;
                    var children = t.ItemType == ItemType.Enum
                        ? t.Members.Cast<ReflectionItem>().ToList()
                        : null;
                    GenerateRequiredMetadata(rval, item, children);
                    break;
                case Namespace n:
                    rval.Namespace = n.Name;
                    rval.FullName = n.Name;
                    GenerateRequiredMetadata(rval, item);
                    break;
            }

            if (item.Metadata.TryGetValue(OPSMetadata.InternalOnly, out object val))
            {
                rval.IsInternalOnly = (bool)val;
            }

            if (item.Metadata.TryGetValue(OPSMetadata.AdditionalNotes, out object notes))
            {
                rval.AdditionalNotes = (AdditionalNotes)notes;
            }

            if (item.Attributes != null && item.Attributes.Any(attr => attr.TypeFullName == "System.ObsoleteAttribute"))
            {
                rval.IsDeprecated = true;
            }

            return rval;
        }

        private void GenerateRequiredMetadata(ItemSDPModelBase model, ReflectionItem item, List<ReflectionItem> childrenItems = null)
        {
            MergeWhiteListedMetadata(model, item);
            if (item.ItemType != ItemType.Namespace)
            {
                ApiScanGenerator.Generate(model, item);
            }
            F1KeywordsGenerator.Generate(model, item, childrenItems);
            HelpViewerKeywordsGenerator.Generate(model, item, childrenItems);
        }

        private IEnumerable<TypeParameter> ConvertTypeParameters(ReflectionItem item)
        {
            if (item.TypeParameters?.Count > 0)
            {
                return item.TypeParameters.Select(tp =>
                    new TypeParameter()
                    {
                        Description = tp.Description,
                        Name = tp.Name
                    }).ToList();
            }
            return null;
        }

        private T ConvertParameter<T>(Parameter p, List<Parameter> knownTypeParams = null, bool showGenericType = true) where T : TypeReference, new()
        {
            var isGeneric = knownTypeParams?.Any(tp => tp.Name == p.Type) ?? false;
            return new T()
            {
                Description = p.Description,
                Type = isGeneric
                    ? (showGenericType ? p.Type : "") // should be `p.Type`, tracked in https://ceapex.visualstudio.com/Engineering/_workitems/edit/72695
                    : TypeStringToTypeMDString(p.OriginalTypeString ?? p.Type, _store)
            };
        }

        private Models.SDP.ThreadSafety ConvertThreadSafety(ReflectionItem item)
        {
            if (item.Docs.ThreadSafetyInfo != null)
            {
                return new Models.SDP.ThreadSafety()
                {
                    CustomizedContent = item.Docs.ThreadSafetyInfo.CustomContent,
                    IsSupported = item.Docs.ThreadSafetyInfo.Supported,
                    MemberScope = item.Docs.ThreadSafetyInfo.MemberScope
                };
            }
            return null;
        }

        public static string BuildSeeAlsoList(Docs docs, ECMAStore store)
        {
            StringBuilder sb = new StringBuilder();
            if (docs.AltMemberCommentIds != null)
            {
                foreach (var altMemberId in docs.AltMemberCommentIds)
                {
                    var uid = altMemberId.ResolveCommentId(store)?.Uid ?? altMemberId.Substring(altMemberId.IndexOf(':') + 1);
                    uid = System.Web.HttpUtility.UrlEncode(uid);
                    sb.AppendLine($"- <xref:{uid}>");
                }
            }
            if (docs.Related != null)
            {
                foreach (var rTag in docs.Related)
                {
                    var uri = rTag.Uri.Contains(' ') ? rTag.Uri.Replace(" ", "%20") : rTag.Uri;
                    sb.AppendLine($"- [{rTag.OriginalText}]({uri})");
                }
            }

            return sb.Length == 0 ? null : sb.ToString();
        }
    }
}
