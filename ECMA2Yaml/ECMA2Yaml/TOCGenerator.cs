using ECMA2Yaml.Models;
using Microsoft.DocAsCode.DataContracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public static class TOCGenerator
    {
        public static TocViewModel Generate(ECMAStore store)
        {
            TocViewModel toc = new TocViewModel();

            foreach (var ns in store.Namespaces.Values)
            {
                if (ns.Types?.Count > 0)
                {
                    toc.Add(GenerateTocItemForNamespace(ns));
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
            if (ns.Metadata!= null && ns.Metadata.ContainsKey(OPSMetadata.Monikers))
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
            };
            if (t.Metadata != null && t.Metadata.ContainsKey(OPSMetadata.Monikers))
            {
                tToc.Metadata[OPSMetadata.Monikers] = t.Metadata[OPSMetadata.Monikers];
            }
            return tToc;
        }
    }
}
