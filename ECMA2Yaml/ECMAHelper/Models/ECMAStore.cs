using Monodoc.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;

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

            _nsList = nsList;
            _tList = nsList.SelectMany(ns => ns.Types).ToList();
            _frameworks = frameworks;
        }

        public void TranslateSourceLocation(string sourcePathRoot, string gitBaseUrl)
        {
            if (!sourcePathRoot.EndsWith("\\"))
            {
                sourcePathRoot += "\\";
            }
            if (!gitBaseUrl.EndsWith("/"))
            {
                gitBaseUrl += "/";
            }
            foreach (var ns in _nsList)
            {
                TranslateSourceLocation(ns, sourcePathRoot, gitBaseUrl);
                foreach (var t in ns.Types)
                {
                    TranslateSourceLocation(t, sourcePathRoot, gitBaseUrl);
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members)
                        {
                            TranslateSourceLocation(m, sourcePathRoot, gitBaseUrl);
                        }
                    }
                }
            }
        }

        private void TranslateSourceLocation(ReflectionItem item, string sourcePathRoot, string gitBaseUrl)
        {
            if (item.Metadata.ContainsKey(OPSMetadata.XMLLocalPath))
            {
                item.Metadata[OPSMetadata.ContentUrl] = ((string)item.Metadata[OPSMetadata.XMLLocalPath]).Replace(sourcePathRoot, gitBaseUrl).Replace("\\", "/");
            }
        }

        public void Build()
        {
            Namespaces = _nsList.ToDictionary(ns => ns.Name);
            TypesByFullName = _tList.ToDictionary(t => t.FullName);

            BuildIds(_nsList, _tList);

            TypesByUid = _tList.ToDictionary(t => t.Uid);
            var allMembers = _tList.Where(t => t.Members != null).SelectMany(t => t.Members).ToList();
            var groups = allMembers.GroupBy(m => m.Uid).Where(g => g.Count() > 1).ToList();
            if (groups.Count > 0)
            {
                foreach(var group in groups)
                {
                    foreach(var member in group)
                    {
                        OPSLogger.LogUserError(string.Format("Member {0}'s name and signature is not unique", member.FullDisplayName), member.Metadata[OPSMetadata.XMLLocalPath].ToString());
                    }
                }
            }
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
                if (_frameworks.ContainsKey(ns.Uid))
                {
                    ns.Metadata[OPSMetadata.Version] = _frameworks[ns.Uid];
                }
                foreach (var t in ns.Types)
                {
                    if (_frameworks.ContainsKey(t.DocId))
                    {
                        t.Metadata[OPSMetadata.Version] = _frameworks[t.DocId];
                    }
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members)
                        {
                            if (_frameworks.ContainsKey(m.DocId))
                            {
                                m.Metadata[OPSMetadata.Version] = _frameworks[m.DocId];
                            }
                            else
                            {
                                throw new Exception(string.Format("Unable to find framework info for {0} {1}", t.Uid, m.Signatures["C#"]));
                            }
                        }
                    }
                }
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

            foreach (var ns in nsList)
            {
                ns.Types = ns.Types.OrderBy(t => t.Uid, new TypeIdComparer()).ToList();
            }
        }

        private void BuildOverload(Type t)
        {
            var methods = t.Members?.Where(m =>
                m.ItemType == ItemType.Method
                || m.ItemType == ItemType.Constructor
                || m.ItemType == ItemType.Property
                || m.ItemType == ItemType.Operator)
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
                            DisplayName = m.ItemType == ItemType.Constructor ? t.Name : m.Name,
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

                if (t.ItemType == ItemType.Class)
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
                                    if (m.Name != "Finalize" && m.ItemType != ItemType.Constructor)
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
            EcmaDesc desc = null;
            if (typeDescriptorCache.ContainsKey(typeString))
            {
                desc = typeDescriptorCache[typeString];
            }
            else if (typeString != null && typeString.EndsWith("*"))
            {
                if (EcmaParser.TryParse("T:" + typeString.TrimEnd('*'), out desc))
                {
                    desc.DescModifier = EcmaDesc.Mod.Pointer;
                    typeDescriptorCache.Add(typeString, desc);
                }
            }
            else if (EcmaParser.TryParse("T:" + typeString, out desc))
            {
                typeDescriptorCache.Add(typeString, desc);
            }
            return desc;
        }
    }
}
