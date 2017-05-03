using ECMA2Yaml.Models;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.DataContracts.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
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
        public const string LandingPageTypeMetadata = "landingPageType";

        public static void Merge(CommandLineOptions opt)
        {
            string outputPath = opt.OutputFolder ?? Path.GetDirectoryName(opt.RefTOCPath);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            var topTOC = YamlUtility.Deserialize<TocViewModel>(opt.TopLevelTOCPath);
            var refTOC = YamlUtility.Deserialize<TocViewModel>(opt.RefTOCPath);
            var refTOCDict = refTOC.ToDictionary(t => t.Name);
            Queue<TocItemViewModel> itemsToGo = new Queue<TocItemViewModel>();
            topTOC.ForEach(t => itemsToGo.Enqueue(t));
            while (itemsToGo.Count > 0)
            {
                var item = itemsToGo.Dequeue();
                if (item.Items != null)
                {
                    item.Items.ForEach(t => itemsToGo.Enqueue(t));
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
                            else
                            {
                                OPSLogger.LogUserWarning(string.Format("Children pattern {0} cannot match any sub TOC", child), opt.TopLevelTOCPath);
                            }
                        }
                    }
                }
            }
            if (refTOCDict.Count > 0)
            {
                foreach(var remainingItem in refTOCDict.Values)
                {
                    topTOC.Add(remainingItem);
                }
            }
            if (!string.IsNullOrEmpty(opt.ConceptualTOCUrl))
            {
                topTOC.First().Metadata[OPSMetadata.Universal_Conceptual_TOC] = opt.ConceptualTOCUrl;
            }
            TrimTOCAndCreateLandingPage(topTOC, outputPath, opt.HideEmptyNode);
            YamlUtility.Serialize(opt.RefTOCPath, topTOC);

            InjectTOCMetadata(opt.ConceptualTOCPath, OPSMetadata.Universal_Ref_TOC, opt.RefTOCUrl);
        }

        private static void TrimTOCAndCreateLandingPage(TocViewModel toc, string outputPath, bool removeEmptyNode = false)
        {
            if (toc != null && toc.Count > 0)
            {
                if (removeEmptyNode)
                {
                    toc.RemoveAll(item => item.Metadata.ContainsKey(ChildrenMetadata) && (item.Items == null || item.Items.Count == 0));
                }

                foreach(var item in toc)
                {
                    item.Metadata.Remove(ChildrenMetadata);
                    TrimTOCAndCreateLandingPage(item.Items, outputPath, removeEmptyNode);
                    if (!string.IsNullOrEmpty(item.Uid) && item.Metadata.ContainsKey(LandingPageTypeMetadata) && item.Items != null)
                    {
                        var page = CreateLandingPage(item);
                        var landingPageType = (string)item.Metadata[LandingPageTypeMetadata];
                        var fileName = (landingPageType == "Root" ? "index" : item.Name.Replace(" ", "")) + ".yml";
                        fileName = Path.Combine(outputPath, fileName);
                        YamlUtility.Serialize(fileName, page, YamlMime.ManagedReference);
                    }
                }
            }
        }

        private static Regex WildCardToRegex(String value)
        {
            return new Regex("^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$", RegexOptions.Compiled);
        }

        private static PageViewModel CreateLandingPage(TocItemViewModel tocItem)
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
