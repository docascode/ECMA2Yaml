using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public static class ConverterHelper
    {
        public static readonly IReadOnlyDictionary<ItemType, string> ItemTypeNameMapping = new Dictionary<ItemType, string>()
        {
            {ItemType.Default, "default"},
            {ItemType.Toc, "toc"},
            {ItemType.Assembly, "assembly"},
            {ItemType.Namespace, "namespace"},
            {ItemType.Class, "class"},
            {ItemType.Interface, "interface"},
            {ItemType.Struct, "struct"},
            {ItemType.Delegate, "delegate"},
            {ItemType.Enum, "enum"},
            {ItemType.Field, "field"},
            {ItemType.Property, "property"},
            {ItemType.Event, "event"},
            {ItemType.Constructor, "constructor"},
            {ItemType.Method, "method"},
            {ItemType.Operator, "operator"},
            {ItemType.Container, "container"},
            {ItemType.AttachedEvent, "attachedevent"},
            {ItemType.AttachedProperty, "attachedproperty"}
        };

        public static SortedList<string, string> BuildSignatures(ReflectionItem item)
        {
            var contents = new SortedList<string, string>();
            if (item.Signatures?.Dict != null)
            {
                foreach (var sigPair in item.Signatures.Dict)
                {
                    if (Models.ECMADevLangs.OPSMapping.ContainsKey(sigPair.Key))
                    {
                        var lang = Models.ECMADevLangs.OPSMapping[sigPair.Key];
                        var val = sigPair.Value.LastOrDefault()?.Value;
                        if (sigPair.Key == ECMADevLangs.CSharp)
                        {
                            var contentBuilder = new StringBuilder();
                            if (item.Attributes?.Count > 0)
                            {
                                foreach (var att in item.Attributes.Where(attr => attr.Visible))
                                {
                                    contentBuilder.AppendFormat("[{0}]\n", att.Declaration);
                                }
                            }
                            contentBuilder.Append(val);
                            contents[lang] = contentBuilder.ToString();
                        }
                        else
                        {
                            contents[lang] = val;
                        }
                    }
                }
            }

            return contents;
        }

        public static List<VersionedSignatureModel> BuildVersionedSignatures(ReflectionItem item)
        {
            var contents = new SortedList<string, List<VersionedString>>();
            if (item.Signatures?.Dict != null)
            {
                foreach (var sigPair in item.Signatures.Dict)
                {
                    if (Models.ECMADevLangs.OPSMapping.ContainsKey(sigPair.Key))
                    {
                        var lang = Models.ECMADevLangs.OPSMapping[sigPair.Key];
                        var sigValues = sigPair.Value;
                        var visibleAttrs = item.Attributes?.Where(attr => attr.Visible).ToList();
                        if (sigPair.Key == ECMADevLangs.CSharp
                            && visibleAttrs?.Count > 0)
                        {
                            bool versionedSig = sigValues.Any(v => v.Monikers?.Count > 0) && sigValues.Count > 1;
                            bool versionedAttr = visibleAttrs.Any(attr => attr.Monikers?.Count > 0);
                            // devide into 2 cases for better perf, most of the time neither signature nor attributes are versioned
                            if (!versionedSig && !versionedAttr)
                            {
                                contents[lang] = new List<VersionedString>() 
                                { 
                                    new VersionedString(null, AttachAttributesToSignature(visibleAttrs, sigValues.First().Value)) 
                                };
                            }
                            else
                            {

                                contents[lang] = item.Monikers.Select(moniker =>
                                {
                                    var sig = sigValues.FirstOrDefault(s => s.Monikers == null || s.Monikers.Contains(moniker));
                                    var attrs = visibleAttrs.Where(a => a.Monikers == null || a.Monikers.Contains(moniker)).ToList();
                                    var combinedSig = AttachAttributesToSignature(attrs, sig?.Value);
                                    return (moniker, combinedSig);
                                })
                                .GroupBy(t => t.combinedSig)
                                .Select(g => new VersionedString(g.Select(t => t.moniker).ToHashSet(), g.Key))
                                .ToList();
                            }
                        }
                        else
                        {
                            contents[lang] = sigValues;
                        }
                    }
                }
            }

            return contents.Select(sig => new VersionedSignatureModel() { Lang = sig.Key, Values = sig.Value }).ToList();
        }

        private static string AttachAttributesToSignature(IEnumerable<ECMAAttribute> attrs, string sig)
        {
            var contentBuilder = new StringBuilder();
            if (attrs?.Any() == true)
            {
                foreach (var att in attrs)
                {
                    contentBuilder.AppendFormat("[{0}]\n", att.Declaration);
                }
            }
            contentBuilder.Append(sig);

            return contentBuilder.ToString();
        }

        public static SortedList<string, string> BuildUWPSignatures(ReflectionItem item)
        {
            var contents = new SortedList<string, string>();
            if (item.Signatures?.Dict != null)
            {
                foreach (var sigPair in item.Signatures.Dict)
                {
                    if (Models.ECMADevLangs.OPSMapping.ContainsKey(sigPair.Key))
                    {
                        var langAlias = Models.ECMADevLangs.OPSMapping[sigPair.Key];
                        var val = sigPair.Value.LastOrDefault()?.Value;
                        switch (sigPair.Key)
                        {
                            case ECMADevLangs.CSharp:
                                contents[langAlias] = UWPCSharpSignatureTransform(val);
                                break;
                            case ECMADevLangs.CPP_CLI:
                            case ECMADevLangs.CPP_CX:
                            case ECMADevLangs.CPP_WINRT:
                                contents[langAlias] = UWPCPPSignatureTransform(val);
                                break;
                            default:
                                contents[langAlias] = val;
                                break;
                        }
                    }
                }
            }

            return contents;
        }

        public static List<VersionedSignatureModel> BuildVersionedUWPSignatures(ReflectionItem item)
        {
            var contents = new SortedList<string, List<VersionedString>>();
            if (item.Signatures?.Dict != null)
            {
                foreach (var sigPair in item.Signatures.Dict)
                {
                    if (Models.ECMADevLangs.OPSMapping.ContainsKey(sigPair.Key))
                    {
                        var langAlias = Models.ECMADevLangs.OPSMapping[sigPair.Key];
                        var sigValues = sigPair.Value;
                        switch (sigPair.Key)
                        {
                            case ECMADevLangs.CSharp:
                                sigValues.ForEach(vs => vs.Value = UWPCSharpSignatureTransform(vs.Value));
                                contents[langAlias] = sigValues;
                                break;
                            case ECMADevLangs.CPP_CLI:
                            case ECMADevLangs.CPP_CX:
                            case ECMADevLangs.CPP_WINRT:
                                sigValues.ForEach(vs => vs.Value = UWPCPPSignatureTransform(vs.Value));
                                contents[langAlias] = sigValues;
                                break;
                            default:
                                contents[langAlias] = sigValues;
                                break;
                        }
                    }
                }
            }

            return contents.Select(sig => new VersionedSignatureModel() { Lang = sig.Key, Values = sig.Value }).ToList();
        }

        public static HashSet<string> TrimMonikers(HashSet<string> propertyMonikers, HashSet<string> itemMonikers)
        {
            if (itemMonikers != null && propertyMonikers != null)
            {
                if (itemMonikers.IsSubsetOf(propertyMonikers))
                {
                    return null;
                }
                else if (propertyMonikers.Overlaps(itemMonikers))
                {
                    return new HashSet<string>(propertyMonikers.Intersect(itemMonikers));
                }
            }
            return propertyMonikers;
        }

        public static List<VersionedValue<T>> TrimMonikers<T>(List<VersionedValue<T>> versionedValues, HashSet<string> itemMonikers)
        {
            if (versionedValues?.Count == 1) //for perf, 95% cases there's no versioning
            {
                versionedValues[0].Monikers = null;
            }
            else if (versionedValues?.Count > 1)
            {
                foreach (var v in versionedValues)
                {
                    v.Monikers = TrimMonikers(v.Monikers, itemMonikers);
                }
            }
            return versionedValues;
        }

        public static List<VersionedCollection<T>> TrimMonikers<T>(List<VersionedCollection<T>> versionedValues, HashSet<string> itemMonikers)
        {
            if (versionedValues?.Count == 1) //for perf, 95% cases there's no versioning
            {
                versionedValues[0].Monikers = null;
            }
            else if (versionedValues?.Count > 1)
            {
                foreach (var v in versionedValues)
                {
                    v.Monikers = TrimMonikers(v.Monikers, itemMonikers);
                }
            }
            return versionedValues;
        }

        public static IEnumerable<string> ConsolidateVersionedValues(IEnumerable<VersionedString> vals, HashSet<string> pageMonikers)
        {
            if (vals == null || vals.Any(v => v.Monikers == null))
            { 
                return null;
            }
            HashSet<string> allMonikers = new HashSet<string>();
            foreach(var val in vals)
            {
                allMonikers.UnionWith(val.Monikers);
                if (val.Monikers.SetEquals(pageMonikers))
                {
                    val.Monikers = null;
                }
            }
            return allMonikers.SetEquals(pageMonikers) ? null : allMonikers;
        }

        private static readonly Regex CSharpSignatureLongNameRegex = new Regex("\\w+(\\.\\w+){2,}", RegexOptions.Compiled);
        private static string UWPCSharpSignatureTransform(string sig)
        {
            var csharpSyntax = CSharpSignatureLongNameRegex.Replace(sig, match =>
            {
                var val = match.Value;
                return val.Substring(val.LastIndexOf('.') + 1);
            });
            return csharpSyntax.Replace(" (", "(");
        }

        private static readonly Regex CPPSignatureLongNameRegex = new Regex("\\w+(::\\w+){2,}", RegexOptions.Compiled);
        private static string UWPCPPSignatureTransform(string sig)
        {
            return CPPSignatureLongNameRegex.Replace(sig, match =>
            {
                var val = match.Value;
                return val.Substring(val.LastIndexOf(':') + 1);
            });
        }
    }
}
