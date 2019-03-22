using ECMA2Yaml.Models;
using Microsoft.DocAsCode.DataContracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public static class SDPTOCGenerator
    {
        public static TocRootViewModel Generate(ECMAStore store)
        {
            TocRootViewModel toc = new TocRootViewModel()
            {
                Items = new TocViewModel()
            };

            foreach (var ns in store.Namespaces.Values)
            {
                if (ns.Types?.Count > 0)
                {
                    toc.Items.Add(GenerateTocItemForNamespace(ns));
                }
            }

            return toc;
        }

        private static TocItemViewModel GenerateTocItemForNamespace(Namespace ns)
        {
            var nsToc = new TocItemViewModel()
            {
                Uid = string.IsNullOrEmpty(ns.Uid) ? null : ns.Uid,
                Name = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name,
                Items = new TocViewModel(ns.Types.Select(t => GenerateTocItemForType(t)).ToList())
            };
            if (ns.Metadata != null && ns.Metadata.ContainsKey(OPSMetadata.Monikers))
            {
                nsToc.Metadata[OPSMetadata.Monikers] = ns.Metadata[OPSMetadata.Monikers];
            }
            return nsToc;
        }

        private static TocItemViewModel GenerateTocItemForType(Models.Type t)
        {
            var tToc = new TocItemViewModel()
            {
                Uid = t.Uid,
                Name = t.Name,
                Items = GenerateTocItemsForMembers(t)
            };
            if (t.Metadata != null && t.Metadata.ContainsKey(OPSMetadata.Monikers))
            {
                tToc.Metadata[OPSMetadata.Monikers] = t.Metadata[OPSMetadata.Monikers];
            }
            return tToc;
        }

        private static TocViewModel GenerateTocItemsForMembers(Models.Type t)
        {
            if (t.Members == null || t.Members.Count == 0)
            {
                return null;
            }

            TocViewModel items = new TocViewModel();
            foreach(var olGroup in t.Members.Where(m => m.Overload != null).GroupBy(m => m.Overload))
            {
                var ol = t.Overloads.FirstOrDefault(o => o.Uid == olGroup.Key);
                var tocEntry = new TocItemViewModel()
                {
                    Uid = ol.Uid,
                    Name = ol.DisplayName
                };
                tocEntry.Metadata["type"] = ol.ItemType.ToString();
                if (ol.ItemType == ItemType.Method && olGroup.First().IsEII)
                {
                    tocEntry.Metadata["isEii"] = true;
                }
                items.Add(tocEntry);
            }
            foreach (var m in t.Members.Where(m => m.Overload == null))
            {
                var tocEntry = new TocItemViewModel()
                {
                    Uid = m.Uid,
                    Name = m.DisplayName
                };
                tocEntry.Metadata["type"] = m.ItemType.ToString();
                items.Add(tocEntry);
            }
            return items;
        }
    }
}
