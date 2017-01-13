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

            var allMembers = tList.Where(t => t.Members != null).SelectMany(t => t.Members).ToList();
            var groups = allMembers.GroupBy(m => m.Uid).Where(g => g.Count() > 1).ToList();
            MembersByUid = allMembers.ToDictionary(m => m.Uid);
        }

        private void BuildIds(IEnumerable<Namespace> nsList, IEnumerable<Type> tList)
        {
            foreach (var ns in nsList)
            {
                ns.BuildId(this);
            }
            foreach (var t in tList)
            {
                t.BuildId(this);
            }
            foreach (var t in tList.Where(x => x.Members?.Count > 0))
            {
                t.Members.ForEach(m => m.BuildId(this));
            }
        }
    }
}
