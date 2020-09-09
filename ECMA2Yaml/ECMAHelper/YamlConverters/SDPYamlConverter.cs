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
                DevLangs = item.Signatures?.DevLangs ?? defaultLangList,

                SeeAlso = BuildSeeAlsoList(item.Docs, _store),
                Summary = item.Docs.Summary,
                Remarks = item.Docs.Remarks,
                Examples = item.Docs.Examples,
                Monikers = item.Monikers,
                Source = (_store.UWPMode || _store.DemoMode) ?item.SourceDetail.ToSDPSourceDetail() : null
            };

            if(_withVersioning)
            {
                rval.AssembliesWithMoniker = _store.UWPMode ? null : MonikerizeAssemblyStrings(item);
                rval.PackagesWithMoniker = _store.UWPMode ? null : MonikerizePackageStrings(item, _store.PkgInfoMapping);
                rval.AttributesWithMoniker = item.Attributes?.Where(att => att.Visible)
                    .Select(att => new VersionedString() { Value = att.TypeFullName, Monikers = att.Monikers?.ToHashSet() })
                    .DistinctVersionedString()
                    .ToList().NullIfEmpty();
                rval.AttributeMonikers = ConverterHelper.ConsolidateVersionedValues(rval.AttributesWithMoniker, item.Monikers);
                rval.SyntaxWithMoniker = ConverterHelper.BuildVersionedSignatures(item, uwpMode: _store.UWPMode)?.NullIfEmpty();
            }
            else
            {
                rval.Assemblies = _store.UWPMode ? null : item.AssemblyInfo?.Select(asm => asm.Name).Distinct().ToList();
                rval.Attributes = item.Attributes?.Where(att => att.Visible).Select(att => att.TypeFullName)
                    .ToList().NullIfEmpty();
                var rawSignatures = ConverterHelper.BuildSignatures(item, uwpMode: _store.UWPMode);
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

            if (item.Attributes != null)
            {
                rval.ObsoleteMessagesWithMoniker = item.Attributes
                    .Where(attr => attr.TypeFullName == "System.ObsoleteAttribute")
                    .Select(attr => new VersionedString() 
                    { 
                        Value = GenerateObsoleteNotification(attr.Declaration),
                        Monikers = attr.Monikers
                    })
                    .ToList().NullIfEmpty();
            }

            if (_store.UWPMode || _store.DemoMode)
            {
                GenerateUWPRequirements(rval, item);
            }

            return rval;
        }

        private string GenerateObsoleteNotification(string declaration)
        {
            var value = "";
            if (string.IsNullOrEmpty(declaration))
            {
                return value;
            }

            var startIndex = declaration.IndexOf("(");
            var endIndex = declaration.LastIndexOf(")");
            if (startIndex == -1 || endIndex == -1)
            {
                return value;
            }

            startIndex = startIndex + 1;
            value = declaration.Substring(startIndex, endIndex - startIndex);
            if (value.Contains(','))
            {
                endIndex= value.LastIndexOf(',');
                value = value.Substring(0, endIndex);
            }

            value=value.TrimStart('"').TrimEnd('"');

            return value;
        }

        private void GenerateUWPRequirements(ItemSDPModelBase model, ReflectionItem item)
        {
            UWPRequirements uwpRequirements = new UWPRequirements();

            if (item.Metadata.TryGetValue(UWPMetadata.DeviceFamilyNames, out object deviceFamilies))
            {
                String[] familyNames = (String[])deviceFamilies;
                List<DeviceFamily> families = new List<DeviceFamily>();
                if (familyNames.Length > 0 && item.Metadata.TryGetValue(UWPMetadata.DeviceFamilyVersions, out object deviceFamilyVersions))
                {
                    String[] familyVersions = (String[])deviceFamilyVersions;

                    if (familyVersions.Length > 0)
                    {
                        int minNameVersionPairs = Math.Min(familyNames.Length, familyVersions.Length);

                        for (int i = 0; i < minNameVersionPairs; i++)
                        {
                            DeviceFamily df = new DeviceFamily { Name = familyNames[i], Version = familyVersions[i] };
                            families.Add(df);
                        }
                    }
                }

                if (families.Count > 0)
                    uwpRequirements.DeviceFamilies = families;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.ApiContractNames, out object apiContracts))
            {
                String[] apicNames = (String[])apiContracts;
                List<APIContract> contracts = new List<APIContract>();
                if (apicNames.Length > 0 && item.Metadata.TryGetValue(UWPMetadata.ApiContractVersions, out object apicVersions))
                {
                    String[] contractVersions = (String[])apicVersions;

                    if (contractVersions.Length > 0)
                    {
                        int minNameVersionPairs = Math.Min(apicNames.Length, contractVersions.Length);

                        for (int i = 0; i < minNameVersionPairs; i++)
                        {
                            APIContract apic = new APIContract { Name = apicNames[i], Version = contractVersions[i] };
                            contracts.Add(apic);
                        }
                    }
                }

                if (contracts.Count > 0)
                    uwpRequirements.APIContracts = contracts;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.SDKRequirementsName, out object sdkReqName))
            {
                SDKRequirements sdkRequirements = new SDKRequirements { Name = (string)sdkReqName };
                if (item.Metadata.TryGetValue(UWPMetadata.SDKRequirementsUrl, out object sdkReqUrl))
                {
                    sdkRequirements.Url = (string)sdkReqUrl;
                }
                model.SDKRequirements = sdkRequirements;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.OSRequirementsName, out object osReqName))
            {
                OSRequirements osRequirements = new OSRequirements { Name = (string)osReqName };
                if (item.Metadata.TryGetValue(UWPMetadata.OSRequirementsMinVersion, out object osReqMinVer))
                {
                    osRequirements.MinVer = (string)osReqMinVer;
                }
                model.OSRequirements = osRequirements;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.Capabilities, out object capabilities))
            {
                model.Capabilities = (IEnumerable<string>)capabilities;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.XamlMemberSyntax, out object xamlMemberSyntax))
            {
                model.XamlMemberSyntax = (string)xamlMemberSyntax;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.XamlSyntax, out object xamlSyntax))
            {
                model.XamlSyntax = (string)xamlSyntax;
            }

            if (uwpRequirements.DeviceFamilies != null
                || uwpRequirements.APIContracts != null)
                model.UWPRequirements = uwpRequirements;
        }

        private void GenerateRequiredMetadata(ItemSDPModelBase model, ReflectionItem item, List<ReflectionItem> childrenItems = null)
        {
            MergeWhiteListedMetadata(model, item);
            if (item.ItemType != ItemType.Namespace)
            {
                ApiScanGenerator.Generate(model, item);
                if (_store.UWPMode)
                {
                    model.Metadata?.Remove(ApiScanGenerator.APISCAN_APILOCATION);
                }
            }
            F1KeywordsGenerator.Generate(model, item, childrenItems);
            HelpViewerKeywordsGenerator.Generate(model, item, childrenItems);
            
            // Per V3 requirement, we need to put page level monikers in metadata node.
            // To make it compatible with V2 and existing template code, we choose to duplicate this meta in both root level and metadata node
            if (model is OverloadSDPModel
                || model is TypeSDPModel
                || model is NamespaceSDPModel
                || model is EnumSDPModel
                || model is DelegateSDPModel)
            {
                model.Metadata[OPSMetadata.Monikers] = model.Monikers;
            }
        }

        private IEnumerable<TypeParameterSDPModel> ConvertTypeParameters(ReflectionItem item)
        {
            if (item.TypeParameters?.Count > 0)
            {
                return item.TypeParameters.Select(tp =>
                    new TypeParameterSDPModel()
                    {
                        Description = tp.Description,
                        Name = tp.Name,
                        IsContravariant = tp.IsContravariant,
                        IsCovariant = tp.IsCovariant
                    }).ToList();
            }
            return null;
        }

        private T ConvertParameter<T>(Parameter p, List<TypeParameter> knownTypeParams = null)
            where T : TypeReference, new()
        {
            var isGeneric = knownTypeParams?.Any(tp => tp.Name == p.Type) ?? false;
            return new T()
            {
                Description = p.Description,
                Type = isGeneric ? p.Type : TypeStringToTypeMDString(p.OriginalTypeString ?? p.Type, _store)
            };
        }

        private ParameterReference ConvertNamedParameter(Parameter p, List<TypeParameter> knownTypeParams = null)
        {
            var r = ConvertParameter<ParameterReference>(p, knownTypeParams);
            if (_withVersioning)
            {
                r.NamesWithMoniker = p.VersionedNames;
            }
            else
            {
                r.Name = p.Name;
            }
            return r;
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
