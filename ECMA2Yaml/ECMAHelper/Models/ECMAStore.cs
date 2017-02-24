using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Monodoc.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public class ECMAStore
    {
        public static EcmaUrlParser EcmaParser = new EcmaUrlParser();
        public Dictionary<string, Namespace> Namespaces { get; set; }
        public Dictionary<string, Type> TypesByFullName { get; set; }
        public Dictionary<string, Type> TypesByUid { get; set; }
        public Dictionary<string, Member> MembersByUid { get; set; }

        private static Dictionary<string, EcmaDesc> typeDescriptorCache;

        private IEnumerable<Namespace> _nsList;
        private IEnumerable<Type> _tList;
        private Dictionary<string, List<string>> _frameworks;

        public ECMAStore(IEnumerable<Namespace> nsList, Dictionary<string, List<string>> frameworks)
        {
            typeDescriptorCache = new Dictionary<string, EcmaDesc>();

            this._nsList = nsList;
            this._tList = nsList.SelectMany(ns => ns.Types).ToList();
            this._frameworks = frameworks;
        }

        public void Build()
        {
            Namespaces = _nsList.ToDictionary(ns => ns.Name);
            TypesByFullName = _tList.ToDictionary(t => t.FullName);

            BuildIds(_nsList, _tList);

            TypesByUid = _tList.ToDictionary(t => t.Uid);
            var allMembers = _tList.Where(t => t.Members != null).SelectMany(t => t.Members).ToList();
            var groups = allMembers.GroupBy(m => m.Uid).Where(g => g.Count() > 1).ToList();
            MembersByUid = allMembers.ToDictionary(m => m.Uid);

            foreach (var t in _tList)
            {
                BuildOverload(t);
                BuildInheritance(t);
                BuildDocs(t);
            }

            BuildFrameworks();
        }

        private void BuildFrameworks()
        {
            foreach (var ns in _nsList)
            {
                List<string> fxN = new List<string>();
                foreach (var t in ns.Types)
                {
                    t.Frameworks = _frameworks.GetOrDefault(t.Uid, null);
                    fxN.AddRange(t.Frameworks);
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members)
                        {
                            var fx = _frameworks.GetOrDefault(t.Uid, m.Signatures["C#"]);
                            if (fx == null)
                            {
                                throw new Exception(string.Format("Unable to find framework info for {0} {1}", t.Uid, m.Signatures["C#"]));
                            }
                            m.Frameworks = fx;
                        }
                    }
                }
                ns.Frameworks = fxN.Distinct().ToList();
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
                t.Members.ForEach(m =>
                {
                    m.BuildId(this);
                    m.BuildName(this);
                });
            }
        }

        private void BuildOverload(Type t)
        {
            var methods = t.Members?.Where(m =>
                m.MemberType == MemberType.Method
                || m.MemberType == MemberType.Constructor
                || m.MemberType == MemberType.Property
                || m.MemberType == MemberType.Operator)
                .ToList();
            var overloads = new Dictionary<string, Member>();
            if (methods?.Count() > 0)
            {
                foreach (var m in methods)
                {
                    string id = m.Name.Replace('.', '#') + "*";
                    string overloadUid = string.Format("{0}.{1}", m.Parent.Uid, id);
                    m.Overload = overloadUid;
                    if (!overloads.ContainsKey(overloadUid))
                    {
                        overloads.Add(overloadUid, new Member()
                        {
                            DisplayName = m.MemberType == MemberType.Constructor ? t.Name : m.Name,
                            Id = id,
                            Parent = t
                        });
                    }
                }
            }
            if (overloads.Count > 0)
            {
                t.Overloads = overloads.Values.ToList();
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
                        OPSLogger.LogUserWarning(string.Format("Type {0} has an external base type {1}", t.FullName, uid), t.FullName);
                        uid = null;
                        break;
                    }
                } while (uid != null);

                t.InheritanceUids.Reverse();

                if (t.MemberType == MemberType.Class)
                {
                    t.InheritedMembers = new Dictionary<string, string>();
                    foreach (var btUid in t.InheritanceUids)
                    {
                        if (TypesByUid.ContainsKey(btUid))
                        {
                            var bt = TypesByUid[btUid];
                            if (bt.Members != null)
                            {
                                foreach (var m in bt.Members)
                                {
                                    if (m.Name != "Finalize" && m.MemberType != MemberType.Constructor)
                                    {
                                        t.InheritedMembers[m.Id] = bt.Uid;
                                    }
                                }
                            }
                        }
                    }
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members)
                        {
                            if (t.InheritedMembers.ContainsKey(m.Id))
                            {
                                t.InheritedMembers.Remove(m.Id);
                            }
                        }
                    }
                }
            }
        }

        private void BuildDocs(Type t)
        {
            if (t.TypeParameters != null && t.Docs?.TypeParameters != null)
            {
                foreach (var tp in t.TypeParameters)
                {
                    tp.Description = t.Docs.TypeParameters.ContainsKey(tp.Name) ? t.Docs.TypeParameters[tp.Name] : null;
                }
            }
            if (t.Members != null)
            {
                foreach (var m in t.Members)
                {
                    if (m.TypeParameters != null && m.Docs?.TypeParameters != null)
                    {
                        foreach (var mtp in m.TypeParameters)
                        {
                            mtp.Description = m.Docs.TypeParameters.ContainsKey(mtp.Name) ? m.Docs.TypeParameters[mtp.Name] : null;
                        }
                    }
                    if (m.Parameters != null && m.Docs?.Parameters != null)
                    {
                        foreach (var mp in m.Parameters)
                        {
                            mp.Description = m.Docs.Parameters.ContainsKey(mp.Name) ? m.Docs.Parameters[mp.Name] : null;
                        }
                    }
                    if (m.ReturnValueType != null && m.Docs?.Returns != null)
                    {
                        m.ReturnValueType.Description = m.Docs.Returns;
                    }
                }
            }
        }

        public static EcmaDesc GetOrAddTypeDescriptor(string typeString)
        {
            if (typeDescriptorCache.ContainsKey(typeString))
            {
                return typeDescriptorCache[typeString];
            }
            else
            {
                EcmaDesc desc = null;
                if (EcmaParser.TryParse("T:" + typeString, out desc))
                {
                    typeDescriptorCache.Add(typeString, desc);
                    return desc;
                }
                return null;
            }
        }
    }
}
