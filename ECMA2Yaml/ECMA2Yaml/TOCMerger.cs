using ECMA2Yaml.Models;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.DataContracts.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public class TOCMerger
    {
        public const string ChildrenMetadata = "children";
        public const string VisibleMetadata = "visible";
        public const string LandingPageTypeMetadata = "landingPageType";

        public static void Merge(CommandLineOptions opt)
        {
            string outputPath = opt.OutputFolder ?? Path.GetDirectoryName(opt.RefTOCPath) ?? Path.GetDirectoryName(opt.TopLevelTOCPath);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            Dictionary<string, object> metadata = ParseMetadataJson(opt.LandingPageMetadata);
            var topTOC = YamlUtility.Deserialize<TocViewModel>(opt.TopLevelTOCPath);
            if (!string.IsNullOrEmpty(opt.RefTOCPath) && File.Exists(opt.RefTOCPath))
            {
                var refTOC = YamlUtility.Deserialize<TocViewModel>(opt.RefTOCPath);
                var refTOCDict = refTOC.ToDictionary(t => t.Name);
                Stack<TocItemViewModel> itemsToGo = new Stack<TocItemViewModel>();
                foreach(var t in topTOC.AsEnumerable().Reverse())
                {
                    itemsToGo.Push(t);
                }
                while (itemsToGo.Count > 0)
                {
                    var item = itemsToGo.Pop();
                    if (item.Items != null)
                    {
                        foreach (var t in item.Items.AsEnumerable().Reverse())
                        {
                            itemsToGo.Push(t);
                        }
                    }
                    if (item.Metadata != null)
                    {
                        if (item.Metadata.ContainsKey(ChildrenMetadata))
                        {
                            var children = (List<object>)item.Metadata[ChildrenMetadata];
                            foreach (var child in children.Cast<string>())
                            {
                                var regex = WildCardToRegex(child);
                                var matched = refTOCDict.Keys.Where(key => regex.IsMatch(key)).ToList();
                                if (matched.Count > 0)
                                {
                                    if (item.Items == null)
                                    {
                                        item.Items = new TocViewModel();
                                    }
                                    foreach (var match in matched)
                                    {
                                        if (refTOCDict[match].Href != null && refTOCDict[match].Href.EndsWith("/"))
                                        {
                                            var subTOCPath = Path.Combine(Path.GetDirectoryName(opt.RefTOCPath), refTOCDict[match].Href, "toc.yml");
                                            if (File.Exists(subTOCPath))
                                            {
                                                InjectTOCMetadata(subTOCPath, OPSMetadata.Universal_Conceptual_TOC, opt.ConceptualTOCUrl);
                                                InjectTOCMetadata(subTOCPath, OPSMetadata.Universal_Ref_TOC, opt.RefTOCUrl);
                                            }
                                        }
                                        item.Items.Add(refTOCDict[match]);
                                        refTOCDict.Remove(match);
                                    }
                                }
                                else if (!opt.HideEmptyNode && child != "*")
                                {
                                    OPSLogger.LogUserWarning(string.Format("Children pattern {0} cannot match any sub TOC", child), opt.TopLevelTOCPath);
                                }
                            }
                        }
                    }
                }
                if (refTOCDict.Count > 0)
                {
                    foreach (var remainingItem in refTOCDict.Values)
                    {
                        topTOC.Add(remainingItem);
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(opt.ConceptualTOCUrl))
            {
                topTOC.First().Metadata[OPSMetadata.Universal_Conceptual_TOC] = opt.ConceptualTOCUrl;
            }
            foreach(var root in topTOC)
            {
                TrimTOCAndCreateLandingPage(root, outputPath, metadata, 1, opt.HideEmptyNode);
            }
            YamlUtility.Serialize(opt.RefTOCPath ?? opt.TopLevelTOCPath, topTOC);

            InjectTOCMetadata(opt.ConceptualTOCPath, OPSMetadata.Universal_Ref_TOC, opt.RefTOCUrl);
        }

        private static Dictionary<string, object> ParseMetadataJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            var metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (metadata != null)
            {
                metadata = metadata.ToDictionary(pair => pair.Key, pair =>
                {
                    if (pair.Value is Newtonsoft.Json.Linq.JArray)
                    {
                        return ((Newtonsoft.Json.Linq.JArray)pair.Value).ToObject<string[]>();
                    }
                    return pair.Value;
                });
            }
            return metadata;
        }

        private static void TrimTOCAndCreateLandingPage(TocItemViewModel item, string outputPath, Dictionary<string, object> metadata, int flattenTopLevels, bool removeEmptyNode = false)
        {
            if (item?.Items?.Count > 0)
            {
                if (removeEmptyNode)
                {
                    item.Items.RemoveAll(it => it.Metadata.ContainsKey(ChildrenMetadata) && (it.Items == null || it.Items.Count == 0));
                }
                item.Items.RemoveAll(it => it.Metadata.ContainsKey(VisibleMetadata) && it.Metadata[VisibleMetadata] is bool && !(bool)it.Metadata[VisibleMetadata]);
                item.Metadata.Remove(ChildrenMetadata);
                item.Metadata.Remove(VisibleMetadata);
                var trimmedName = item.Name.Replace(" ", "").Trim();
                var outputPathForChild = flattenTopLevels > 0 ? outputPath : Path.Combine(outputPath, trimmedName);
                foreach (var child in item.Items)
                {
                    TrimTOCAndCreateLandingPage(child, outputPathForChild, metadata, flattenTopLevels - 1, removeEmptyNode);
                }

                if (!string.IsNullOrEmpty(item.Uid) && item.Metadata.ContainsKey(LandingPageTypeMetadata) && (item.Items != null || !removeEmptyNode))
                {
                    var page = CreateLandingPage(item, metadata);
                    var landingPageType = (string)item.Metadata[LandingPageTypeMetadata];
                    var fileName = (landingPageType == "Root" ? "index" : trimmedName) + ".yml";
                    fileName = Path.Combine(outputPath, fileName);
                    YamlUtility.Serialize(fileName, page, YamlMime.ManagedReference);
                }
            }
        }

        private static Regex WildCardToRegex(String value)
        {
            return new Regex("^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$", RegexOptions.Compiled);
        }

        private static PageViewModel CreateLandingPage(TocItemViewModel tocItem, Dictionary<string, object> metadata)
        {
            var validChildren = tocItem.Items.Where(i => !string.IsNullOrEmpty(i.Uid)).ToList();
            var item = new ItemViewModel()
            {
                Id = tocItem.Uid,
                Uid = tocItem.Uid,
                Name = tocItem.Name,
                NameWithType = tocItem.Name,
                FullName = tocItem.Name,
                Type = MemberType.Container,
                Children = validChildren.Select(t => t.Uid).ToList(),
            };
            if (metadata != null)
            {
                foreach(var meta in metadata)
                {
                    if (meta.Key == "langs")
                    {
                        item.SupportedLanguages = meta.Value as string[];
                    }
                    else
                    {
                        item.Metadata.Add(meta.Key, meta.Value);
                    }
                }
            }
            var refs = validChildren.Select(i => new ReferenceViewModel()
            {
                Name = i.Name,
                Uid = i.Uid
            }).ToList();

            var page = new PageViewModel()
            {
                Items = new List<ItemViewModel>()
                {
                    item
                },
                References = refs
            };

            page.Metadata.Add(LandingPageTypeMetadata, tocItem.Metadata[LandingPageTypeMetadata]);
            page.Metadata.Add(OPSMetadata.OpenToPublic, false);
            return page;
        }

        private static void InjectTOCMetadata(string tocPath, string metaName, string metaValue)
        {
            if (!string.IsNullOrEmpty(tocPath) && File.Exists(tocPath))
            {
                var toc = YamlUtility.Deserialize<TocViewModel>(tocPath);
                if (toc != null && toc.Count > 0)
                {
                    toc.First().Metadata[metaName] = metaValue;
                }
                YamlUtility.Serialize(tocPath, toc);
            }
        }
    }
}
