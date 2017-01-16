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
                toc.Add(GenerateTocItemForNamespace(ns));
            }

            return toc;
        }

        private static TocItemViewModel GenerateTocItemForNamespace(Namespace ns)
        {
            var nsToc = new TocItemViewModel()
            {
                Uid = ns.Uid,
                Name = ns.Name,
                Items = new TocViewModel(ns.Types.Select(t => GenerateTocItemForType(t)).ToList())
            };

            return nsToc;
        }

        private static TocItemViewModel GenerateTocItemForType(Models.Type t)
        {
            var tToc = new TocItemViewModel()
            {
                Uid = t.Uid,
                Name = t.Name,
            };

            return tToc;
        }
    }
}
