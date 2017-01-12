using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public class ECMAStore
    {
        public Dictionary<string, Namespace> Namespaces { get; set; }
        public Dictionary<string, Type> TypesByFullName { get; set; }
        public Dictionary<string, Member> MembersByUid { get; set; }

        public ECMAStore(IEnumerable<Namespace> nsList, IEnumerable<Type> tList)
        {
            Namespaces = nsList.ToDictionary(ns => ns.Name);
            TypesByFullName = tList.ToDictionary(t => t.FullName);
            BuildIds(nsList, tList);
        }

        private void BuildIds(IEnumerable<Namespace> nsList, IEnumerable<Type> tList)
        {
            foreach (var ns in nsList)
            {
                if (string.IsNullOrEmpty(ns.Id))
                {
                    ns.Id = ns.Name;
                }
            }
            foreach (var t in tList)
            {
                if (string.IsNullOrEmpty(t.Id))
                {
                    t.Id = t.Name;
                    if (t.TypeParameters?.Count > 0)
                    {
                        t.Id = t.Name.Substring(0, t.Name.IndexOf('<')) + '`' + t.TypeParameters.Count;
                    }
                }
            }
            foreach (var t in tList)
            {
                if (t.Members?.Count > 0)
                {
                    t.Members.ForEach(m => m.BuildId(this));
                }
            }
        }
    }
}
