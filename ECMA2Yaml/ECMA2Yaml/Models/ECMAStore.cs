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

            BuildReferences(nsList, tList);

            var allMembers = tList.Where(t => t.Members != null).SelectMany(t => t.Members).ToList();
            var groups = allMembers.GroupBy(m => m.Uid).Where(g => g.Count() > 1).ToList();
            MembersByUid = allMembers.ToDictionary(m => m.Uid);

            foreach (var t in tList)
            {
                BuildOverload(t);
            }
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

        private void BuildReferences(IEnumerable<Namespace> nsList, IEnumerable<Type> tList)
        {
            foreach (var ns in nsList)
            {
                if (ns.References == null)
                {
                    ns.References = new List<ReflectionItem>();
                }
                ns.References.AddRange(ns.Types);
            }
            foreach (var t in tList)
            {
                if (t.References == null)
                {
                    t.References = new List<ReflectionItem>();
                }
                t.References.AddRange(t.Members);
                t.References.AddRange(t.Members.SelectMany(m => BuildMemberReferences(m)));
                BuildOverload(t);
            }
        }

        private List<ReflectionItem> BuildMemberReferences(Member m)
        {
            var refs = new List<ReflectionItem>();
            var r = BuildRefByTypeString(m.ReturnValueType);
            if (r != null)
            {
                m.ReturnValueType = r.Uid;
                refs.Add(r);
            }
            if (m.Parameters != null)
            {
                foreach (var p in m.Parameters)
                {
                    r = BuildRefByTypeString(p.Type);
                    if (r != null)
                    {
                        p.Type = r.Uid;
                        refs.Add(r);
                    }
                }
            }
            
            return refs;
        }

        private ReflectionItem BuildRefByTypeString(string str)
        {
            if (TypesByFullName.ContainsKey(str))
            {
                return TypesByFullName[str];
            }

            return null;
        }

        private void BuildOverload(Type t)
        {
            var methods = t.Members?.Where(m => m.Type == MemberType.Method).ToList();
            var overloads = new List<Member>();
            if (methods?.Count() > 0)
            {
                foreach (var group in methods.GroupBy(m => m.Name).Where(g => g.Count() > 1))
                {
                    string id = group.First().Name.Replace('.', '#');
                    string overloadUid = string.Format("{0}.{1}*", group.First().Parent.Uid, id);
                    foreach (var method in group)
                    {
                        method.Overload = overloadUid;
                    }
                    overloads.Add(new Member()
                    {
                        Name = methods.First().Type == MemberType.Constructor ? t.Name : methods.First().Name,
                        Id = id
                    });
                }
            }
            if (overloads.Count > 0)
            {
                t.References.AddRange(overloads);
            }
        }
    }
}
