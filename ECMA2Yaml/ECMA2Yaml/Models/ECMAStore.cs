using Microsoft.DocAsCode.DataContracts.ManagedReference;
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
        public Dictionary<string, Type> TypesByUid { get; set; }
        public Dictionary<string, Member> MembersByUid { get; set; }

        public ECMAStore(IEnumerable<Namespace> nsList, IEnumerable<Type> tList)
        {
            Namespaces = nsList.ToDictionary(ns => ns.Name);
            TypesByFullName = tList.ToDictionary(t => t.FullName);

            BuildIds(nsList, tList);

            TypesByUid = tList.ToDictionary(t => t.Uid);
            var allMembers = tList.Where(t => t.Members != null).SelectMany(t => t.Members).ToList();
            var groups = allMembers.GroupBy(m => m.Uid).Where(g => g.Count() > 1).ToList();
            MembersByUid = allMembers.ToDictionary(m => m.Uid);

            foreach (var t in tList)
            {
                BuildOverload(t);
                BuildInheritance(t);
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
                if (t.BaseType != null)
                {
                    t.BaseType.BuildId(this);
                }
            }
            foreach (var t in tList.Where(x => x.Members?.Count > 0))
            {
                t.Members.ForEach(m => m.BuildId(this));
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
            var methods = t.Members?.Where(m => m.MemberType == MemberType.Method).ToList();
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
                        Name = methods.First().MemberType == MemberType.Constructor ? t.Name : methods.First().Name,
                        Id = id,
                        Parent = t
                    });
                }
            }
            if (overloads.Count > 0)
            {
                t.Overloads = new List<Member>();
                t.Overloads.AddRange(overloads);
            }
        }

        private void BuildInheritance(Type t)
        {
            if (t.BaseType != null)
            {
                t.InheritanceUids = new List<string>();
                string uid = t.BaseType.Uid;
                do
                {
                    t.InheritanceUids.Add(uid);
                    if (TypesByUid.ContainsKey(uid))
                    {
                        var tb = TypesByUid[uid];
                        uid = tb.BaseType?.Uid;
                    }
                    else
                    {
                        //throw new Exception("External base type uid detected: " + uid);
                        uid = null;
                        break;
                    }
                } while (uid != null);
            }
        }
    }
}
