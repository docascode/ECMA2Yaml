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

        public static void Merge(string topLevelTOCPath, string refTOCPath, string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.GetDirectoryName(refTOCPath);
            }
            var topTOC = YamlUtility.Deserialize<TocViewModel>(topLevelTOCPath);
            var refTOC = YamlUtility.Deserialize<TocViewModel>(refTOCPath);
            var refTOCDict = refTOC.ToDictionary(t => t.Name);
            Stack<TocItemViewModel> itemsToGo = new Stack<TocItemViewModel>();
            topTOC.ForEach(t => itemsToGo.Push(t));
            while (itemsToGo.Count > 0)
            {
                var item = itemsToGo.Pop();
                if (item.Items != null)
                {
                    item.Items.ForEach(t => itemsToGo.Push(t));
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
                                    item.Items.Add(refTOCDict[match]);
                                    refTOCDict.Remove(match);
                                }
                            }
                            else
                            {
                                OPSLogger.LogUserError(string.Format("Children pattern {0} cannot match any sub TOC", child), topLevelTOCPath);
                            }
                        }
                        item.Metadata.Remove(ChildrenMetadata);
                    }
                    if (!string.IsNullOrEmpty(item.Uid) && item.Metadata.ContainsKey(LandingPageTypeMetadata))
                    {
                        var page = CreateLandingPage(item);
                        var fileName = Path.Combine(outputPath, item.Uid + ".yml");
                        YamlUtility.Serialize(fileName, page, YamlMime.ManagedReference);
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

            YamlUtility.Serialize(refTOCPath, topTOC);
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
                Uid = tocItem.Uid,
                Name = tocItem.Name,
                NameWithType = tocItem.Name,
                FullName = tocItem.Name,
                Type = MemberType.Default,
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
    }
}
