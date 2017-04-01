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
        public Dictionary<string, List<string>> InheritanceParentsByUid { get; set; }
        public Dictionary<string, List<string>> InheritanceChildrenByUid { get; set; }
        public Dictionary<string, ExtensionMethod> ExtensionMethodsByMemberDocId { get; set; }
        public ILookup<string, ExtensionMethod> ExtensionMethodUidsByTargetUid { get; set; }
        public FilterStore FilterStore { get; set; }
        public bool StrictMode { get; set; }

        private static Dictionary<string, EcmaDesc> typeDescriptorCache;

        private IEnumerable<Namespace> _nsList;
        private IEnumerable<Type> _tList;
        private Dictionary<string, List<string>> _frameworks;
        private List<ExtensionMethod> _extensionMethods;

        public ECMAStore(IEnumerable<Namespace> nsList, Dictionary<string, List<string>> frameworks, List<ExtensionMethod> extensionMethods)
        {
            typeDescriptorCache = new Dictionary<string, EcmaDesc>();

            _nsList = nsList;
            _tList = nsList.SelectMany(ns => ns.Types).ToList();
            _frameworks = frameworks;
            _extensionMethods = extensionMethods;

            InheritanceParentsByUid = new Dictionary<string, List<string>>();
            InheritanceChildrenByUid = new Dictionary<string, List<string>>();
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
                        if (t.Overloads != null)
                        {
                            foreach (var o in t.Overloads)
                            {
                                TranslateSourceLocation(o, sourcePathRoot, gitBaseUrl);
                            }
                        }
                    }
                }
            }
        }

        private void TranslateSourceLocation(ReflectionItem item, string sourcePathRoot, string gitBaseUrl)
        {
            if (!string.IsNullOrEmpty(item.SourceFileLocalPath))
            {
                item.Metadata[OPSMetadata.ContentUrl] = item.SourceFileLocalPath.Replace(sourcePathRoot, gitBaseUrl).Replace("\\", "/");
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
                foreach (var group in groups)
                {
                    foreach (var member in group)
                    {
                        OPSLogger.LogUserError(string.Format("Member {0}'s name and signature is not unique", member.Uid), member.SourceFileLocalPath);
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

            BuildAttributes();

            BuildExtensionMethods();

            BuildFrameworks();
        }

        private void BuildExtensionMethods()
        {
            ExtensionMethodsByMemberDocId = _extensionMethods.ToDictionary(ex => ex.MemberDocId);

            foreach(var m in MembersByUid.Values)
            {
                if (ExtensionMethodsByMemberDocId.ContainsKey(m.DocId))
                {
                    m.IsExtensionMethod = true;
                    ExtensionMethodsByMemberDocId[m.DocId].Uid = m.Uid;
                }
            }

            ExtensionMethodUidsByTargetUid = _extensionMethods.ToLookup(ex => ex.TargetDocId.Replace("T:", ""));
            foreach(var ex in _extensionMethods.Where(ex => ex.Uid == null))
            {
                OPSLogger.LogUserWarning(string.Format("ExtensionMethod {0} not found in its type {1}", ex.MemberDocId, ex.ParentType), "index.xml");
            }

            foreach (var t in _tList)
            {
                List<string> extensionMethods = new List<string>();
                Stack<string> uidsToCheck = new Stack<string>();
                uidsToCheck.Push(t.Uid);
                while(uidsToCheck.Count > 0)
                {
                    var uid = uidsToCheck.Pop();
                    if (InheritanceParentsByUid.ContainsKey(uid))
                    {
                        InheritanceParentsByUid[uid].ForEach(u => uidsToCheck.Push(u));
                    }
                    if (ExtensionMethodUidsByTargetUid.Contains(uid))
                    {
                        extensionMethods.AddRange(ExtensionMethodUidsByTargetUid[uid].Where(ex => !string.IsNullOrEmpty(ex.Uid)).Select(ex => ex.Uid));
                    }
                }
                if (extensionMethods.Count > 0)
                {
                    t.ExtensionMethods = extensionMethods.Distinct().ToList();
                    t.ExtensionMethods.Sort();
                }
            }
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
                                OPSLogger.LogUserError(string.Format("Unable to find framework info for {0}", m.DocId), m.SourceFileLocalPath);
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
                ns.Build(this);
            }
            foreach (var t in tList)
            {
                t.Build(this);
                if (t.BaseType != null)
                {
                    t.BaseType.Build(this);
                }
            }
            foreach (var t in tList.Where(x => x.Members?.Count > 0))
            {
                t.Members.ForEach(m =>
                {
                    m.Build(this);
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
            var overloads = t.Overloads?.ToDictionary(o => o.Name) ?? new Dictionary<string, Member>();
            if (methods?.Count() > 0)
            {
                foreach (var m in methods)
                {
                    string id = m.Id;
                    if (id.Contains("("))
                    {
                        id = id.Substring(0, id.IndexOf("("));
                    }
                    id += "*";
                    if (!overloads.ContainsKey(m.Name))
                    {
                        overloads.Add(m.Name, new Member()
                        {
                            Name = m.Name,
                            Parent = t
                        });
                    }
                    overloads[m.Name].Id = id;
                    overloads[m.Name].DisplayName = m.ItemType == ItemType.Constructor ? t.Name : m.Name;
                    overloads[m.Name].SourceFileLocalPath = m.SourceFileLocalPath;
                    m.Overload = overloads[m.Name].Uid;
                }
            }
            if (overloads.Count > 0)
            {
                t.Overloads = overloads.Values.ToList();
            }
        }

        private void BuildAttributes()
        {
            foreach(var t in _tList)
            {
                if (t.Attributes?.Count > 0)
                {
                    t.Attributes.ForEach(attr => ResolveAttribute(attr));
                }
                if (t.Members?.Count > 0)
                {
                    foreach (var m in t.Members)
                    {
                        if (m.Attributes?.Count > 0)
                        {
                            m.Attributes.ForEach(attr => ResolveAttribute(attr));
                        }
                    }
                }
            }
        }

        private void ResolveAttribute(ECMAAttribute attr)
        {
            var fqn = attr.Declaration;
            if (fqn.Contains("("))
            {
                fqn = fqn.Substring(0, fqn.IndexOf("("));
            }
            var nameWithSuffix = fqn + "Attribute";
            if (TypesByFullName.ContainsKey(nameWithSuffix))
            {
                fqn = nameWithSuffix;
            }
            attr.TypeFullName = fqn;
            if (TypesByFullName.ContainsKey(fqn))
            {
                var t = TypesByFullName[fqn];
                if (FilterStore?.AttributeFilters?.Count > 0)
                {
                    foreach (var f in FilterStore.AttributeFilters)
                    {
                        var result = f.Filter(t);
                        if (result.HasValue)
                        {
                            attr.Visible = result.Value;
                        }
                    }
                }
            }
        }

        private void AddInheritanceMapping(string childUid, string parentUid)
        {
            if (!InheritanceParentsByUid.ContainsKey(childUid))
            {
                InheritanceParentsByUid.Add(childUid, new List<string>());
            }
            InheritanceParentsByUid[childUid].Add(parentUid);

            if (!InheritanceChildrenByUid.ContainsKey(parentUid))
            {
                InheritanceChildrenByUid.Add(parentUid, new List<string>());
            }
            InheritanceChildrenByUid[parentUid].Add(childUid);
        }

        private void BuildInheritance(Type t)
        {
            if (t.Interfaces?.Count > 0)
            {
                foreach (var f in t.Interfaces)
                {
                    AddInheritanceMapping(t.Uid, f.ToOuterTypeUid());
                }
            }
            if (t.BaseType != null)
            {
                t.InheritanceUids = new List<string>();
                string baseUid = t.BaseType.Uid;
                AddInheritanceMapping(t.Uid, baseUid);
                do
                {
                    t.InheritanceUids.Add(baseUid);
                    if (TypesByUid.ContainsKey(baseUid))
                    {
                        var tb = TypesByUid[baseUid];
                        baseUid = tb.BaseType?.Uid;
                    }
                    else
                    {
                        if (StrictMode)
                        {
                            OPSLogger.LogUserWarning(string.Format("Type {0} has an external base type {1}", t.FullName, baseUid), t.SourceFileLocalPath);
                        }
                        baseUid = null;
                        break;
                    }
                } while (baseUid != null);

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
                    if (StrictMode && m.Docs?.Exceptions != null)
                    {
                        foreach (var ex in m.Docs?.Exceptions)
                        {
                            if (!TypesByUid.ContainsKey(ex.Uid) && !MembersByUid.ContainsKey(ex.Uid))
                            {
                                OPSLogger.LogUserWarning("Referenced exception type not found: " + ex.Uid, m.SourceFileLocalPath);
                            }
                        }
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
