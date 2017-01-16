using ECMA2Yaml.Models;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Microsoft.DocAsCode.DataContracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public class TopicGenerator
    {
        public Dictionary<string, PageViewModel> GenerateNamespacePages(ECMAStore store)
        {
            var rval = new Dictionary<string, PageViewModel>();

            foreach(var ns in store.Namespaces)
            {
                rval.Add(ns.Key, ns.Value.ToPageViewModel());
            }

            return rval;
        }
    }

    public static class ModalConversionExtensions
    {
        public static PageViewModel ToPageViewModel(this Namespace ns)
        {
            var pv = new PageViewModel();
            pv.Items = new List<ItemViewModel>()
            {
                ns.ToItemViewModel()
            };
            pv.References = ns.Types.Select(t => t.ToReferenceViewModel()).ToList();
            return pv;
        }

        public static ItemViewModel ToItemViewModel(this Namespace ns)
        {
            var item = new ItemViewModel()
            {
                Id = ns.Id,
                Uid = ns.Uid,
                Name = ns.Name,
                NameWithType = ns.Name,
                FullName = ns.Name,
                Type = Microsoft.DocAsCode.DataContracts.ManagedReference.MemberType.Namespace,
                Children = ns.Types.Select(t => t.Uid).ToList()
            };
            return item;
        }

        public static ReferenceViewModel ToReferenceViewModel(this Models.Type t)
        {
            return new ReferenceViewModel()
            {
                Uid = t.Uid,
                Parent = t.Parent.Uid,
                IsExternal = false,
                Name = t.Name,
                NameWithType = t.Name,
                FullName = t.FullName
            };
        }
    }
}
