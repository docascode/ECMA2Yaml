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
        public Dictionary<string, ReflectionItem> ItemsByDocId { get; set; }
        public Dictionary<string, List<string>> InheritanceParentsByUid { get; set; }
        public Dictionary<string, List<string>> InheritanceChildrenByUid { get; set; }
        public Dictionary<string, ExtensionMethod> ExtensionMethodsByMemberDocId { get; set; }
        public ILookup<string, ExtensionMethod> ExtensionMethodUidsByTargetUid { get; set; }
        public FilterStore FilterStore { get; set; }
        public bool StrictMode { get; set; }

        private static Dictionary<string, EcmaDesc> typeDescriptorCache;

        private IEnumerable<Namespace> _nsList;
        private IEnumerable<Type> _tList;
        private FrameworkIndex _frameworks;
        private List<ExtensionMethod> _extensionMethods;
        private Dictionary<string, string> _monikerNugetMapping;
        private Dictionary<string, List<string>> _monikerAssemblyMapping;
        private Dictionary<string, List<string>> _assemblyMonikerMapping;

        public ECMAStore(IEnumerable<Namespace> nsList,
            FrameworkIndex frameworks,
            List<ExtensionMethod> extensionMethods,
            Dictionary<string, string> monikerNugetMapping = null,
            Dictionary<string, List<string>> monikerAssemblyMapping = null)
        {
            typeDescriptorCache = new Dictionary<string, EcmaDesc>();

            _nsList = nsList;
            _tList = nsList.SelectMany(ns => ns.Types).ToList();
            _frameworks = frameworks;
            _extensionMethods = extensionMethods;
            _monikerNugetMapping = monikerNugetMapping;
            _monikerAssemblyMapping = monikerAssemblyMapping;

            InheritanceParentsByUid = new Dictionary<string, List<string>>();
            InheritanceChildrenByUid = new Dictionary<string, List<string>>();
        }

        public void Build()
        {
            Namespaces = _nsList.ToDictionary(ns => ns.Name);
            TypesByFullName = _tList.ToDictionary(t => t.FullName);

            BuildIds(_nsList, _tList);

            TypesByUid = _tList.ToDictionary(t => t.Uid);
            BuildUniqueMembers();
            BuildDocIdDictionary();

            foreach (var t in _tList)
            {
                BuildOverload(t);
                BuildInheritance(t);
                BuildDocs(t);
            }

            FindMissingAssemblyNamesAndVersions();

            BuildAttributes();

            BuildFrameworks();

            BuildExtensionMethods();

            BuildOtherMetadata();
        }

        public void BuildDocIdDictionary()
        {
            ItemsByDocId = new Dictionary<string, ReflectionItem>();
            foreach (var item in TypesByUid.Values.Cast<ReflectionItem>()
                .Concat(MembersByUid.Values.Cast<ReflectionItem>()))
            {
                if (string.IsNullOrEmpty(item.DocId))
                {
                    OPSLogger.LogUserError($"DocId is required for {item.Name}.", item.SourceFileLocalPath);
                }
                else if (ItemsByDocId.ContainsKey(item.DocId))
                {
                    OPSLogger.LogUserError($"Duplicated DocId found: {item.DocId}.", item.SourceFileLocalPath);
                    OPSLogger.LogUserError($"Duplicated DocId found: {item.DocId}.", ItemsByDocId[item.DocId].SourceFileLocalPath);
                }
                else
                {
                    ItemsByDocId.Add(item.DocId, item);
                }
            }
        }

        public void BuildUniqueMembers()
        {
            var allMembers = _tList.Where(t => t.Members != null).SelectMany(t => t.Members).ToList();
            var groups = allMembers.GroupBy(m => m.Uid).Where(g => g.Count() > 1).ToList();
            if (groups.Count > 0)
            {
                foreach (var group in groups)
                {
                    foreach (var member in group)
                    {
                        OPSLogger.LogUserWarning(string.Format("Member {0}'s name and signature is not unique", member.Name), member.SourceFileLocalPath);
                    }
                }
            }

            MembersByUid = new Dictionary<string, Member>();
            var typesToLower = TypesByUid.ToDictionary(p => p.Key.ToLower(), p => p.Value);
            foreach (var member in allMembers)
            {
                if (typesToLower.ContainsKey(member.Uid.ToLower()))
                {
                    member.Id = member.Id + "_" + member.ItemType.ToString().Substring(0, 1).ToLower();
                }
                MembersByUid[member.Uid] = member;
            }
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
            if (!string.IsNullOrEmpty(item.SourceFileLocalPath)
                && item.SourceFileLocalPath.StartsWith(sourcePathRoot)
                && !item.Metadata.ContainsKey(OPSMetadata.ContentUrl))
            {
                var contentGitUrl = item.SourceFileLocalPath.Replace(sourcePathRoot, gitBaseUrl).Replace("\\", "/");
                item.Metadata[OPSMetadata.ContentUrl] = contentGitUrl;
                item.Metadata[OPSMetadata.OriginalContentUrl] = contentGitUrl;
                item.Metadata[OPSMetadata.RefSkeletionUrl] = item.Metadata[OPSMetadata.ContentUrl];
            }
        }

        private void BuildOtherMetadata()
        {
            if (_monikerAssemblyMapping != null)
            {
                _assemblyMonikerMapping = _monikerAssemblyMapping.SelectMany(p => p.Value.Select(v => Tuple.Create(p.Key, v)))
                    .GroupBy(p => p.Item2).ToDictionary(g => g.Key, g => g.Select(p => p.Item1).ToList());
            }
            foreach (var ns in _nsList)
            {
                bool nsInternalOnly = ns.Docs?.InternalOnly ?? false;
                AddAdditionalNotes(ns);
                if (!string.IsNullOrEmpty(ns.Docs?.AltCompliant))
                {
                    ns.Metadata[OPSMetadata.AltCompliant] = ns.Docs?.AltCompliant;
                }
                if (nsInternalOnly)
                {
                    ns.Metadata[OPSMetadata.InternalOnly] = nsInternalOnly;
                }
                if (_monikerNugetMapping != null && ns.Metadata.ContainsKey(OPSMetadata.Monikers))
                {
                    var monikers = (List<string>)ns.Metadata[OPSMetadata.Monikers];
                    List<string> packages = new List<string>();
                    foreach (var moniker in monikers)
                    {
                        if (_monikerNugetMapping.ContainsKey(moniker))
                        {
                            packages.Add(_monikerNugetMapping[moniker]);
                        }
                    }
                    if (packages.Count > 0)
                    {
                        ns.Metadata[OPSMetadata.NugetPackageNames] = packages.ToArray();
                    }
                }
                foreach (var t in ns.Types)
                {
                    BuildAssemblyMonikerMapping(t);
                    AddAdditionalNotes(t);
                    bool tInternalOnly = t.Docs?.InternalOnly ?? nsInternalOnly;
                    if (!string.IsNullOrEmpty(t.Docs?.AltCompliant))
                    {
                        t.Metadata[OPSMetadata.AltCompliant] = t.Docs?.AltCompliant;
                    }
                    if (tInternalOnly)
                    {
                        t.Metadata[OPSMetadata.InternalOnly] = tInternalOnly;
                    }
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members)
                        {
                            bool mInternalOnly = m.Docs?.InternalOnly ?? tInternalOnly;
                            if (!string.IsNullOrEmpty(m.Docs?.AltCompliant))
                            {
                                m.Metadata[OPSMetadata.AltCompliant] = m.Docs?.AltCompliant;
                            }
                            if (mInternalOnly)
                            {
                                m.Metadata[OPSMetadata.InternalOnly] = mInternalOnly;
                            }
                            AddAdditionalNotes(m);
                        }
                    }
                }
            }
        }

        private void AddAdditionalNotes(ReflectionItem item)
        {
            if (item?.Docs.AdditionalNotes != null)
            {
                AdditionalNotes notes = new AdditionalNotes();
                foreach (var note in item.Docs.AdditionalNotes)
                {
                    var val = note.Value.TrimEnd();
                    switch (note.Key)
                    {
                        case "usage":
                            notes.Caller = val;
                            break;
                        case "overrides":
                            if (item.ItemType == ItemType.Interface || item.Parent?.ItemType == ItemType.Interface)
                            {
                                notes.Implementer = val;
                            }
                            else if (item.ItemType == ItemType.Class || item.Parent?.ItemType == ItemType.Class)
                            {
                                notes.Inheritor = val;
                            }
                            break;
                        default:
                            OPSLogger.LogUserWarning("Can't recognize additional notes type: " + note.Key, item.SourceFileLocalPath);
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(notes.Caller)
                    || !string.IsNullOrEmpty(notes.Implementer)
                    || !string.IsNullOrEmpty(notes.Inheritor))
                {
                    item.Metadata[OPSMetadata.AdditionalNotes] = notes;
                }
            }
        }

        private void BuildAssemblyMonikerMapping(ReflectionItem item)
        {
            if (item.VersionedAssemblyInfo != null)
            {
                var dict = item.VersionedAssemblyInfo.MonikersPerValue
                    .GroupBy(p => p.Key.Name)
                    .OrderBy(p => p.Key)
                    .ToDictionary(
                        g => g.Key,
                        g => {
                            var monikers = g.SelectMany(p => p.Value).ToList();
                            monikers.Sort();
                            return monikers;
                        });                
                if (dict.Any())
                {
                    item.Metadata[OPSMetadata.AssemblyMonikerMapping] = dict;
                }
            }
        }

        private void BuildExtensionMethods()
        {
            if (_extensionMethods == null || _extensionMethods.Count == 0)
            {
                return;
            }
            ExtensionMethodsByMemberDocId = _extensionMethods.ToDictionary(ex => ex.MemberDocId);

            foreach (var m in MembersByUid.Values)
            {
                if (!string.IsNullOrEmpty(m.DocId) && ExtensionMethodsByMemberDocId.ContainsKey(m.DocId))
                {
                    m.IsExtensionMethod = true;
                    ExtensionMethodsByMemberDocId[m.DocId].Uid = m.Uid;
                    ExtensionMethodsByMemberDocId[m.DocId].ParentType = m.Parent;
                }
            }

            ExtensionMethodUidsByTargetUid = _extensionMethods.ToLookup(ex => ex.TargetDocId.Replace("T:", ""));
            foreach (var ex in _extensionMethods.Where(ex => ex.Uid == null))
            {
                OPSLogger.LogUserInfo(string.Format("ExtensionMethod {0} not found in its type {1}", ex.MemberDocId, ex.ParentTypeString), "index.xml");
            }

            foreach (var t in _tList)
            {
                List<string> extensionMethods = new List<string>();
                List<string> typeMonikers = null;
                if (t.Metadata.TryGetValue(OPSMetadata.Monikers, out object tMonikers))
                {
                    typeMonikers = tMonikers as List<string>;
                }
                Stack<string> uidsToCheck = new Stack<string>();
                uidsToCheck.Push(t.Uid);
                while (uidsToCheck.Count > 0)
                {
                    var uid = uidsToCheck.Pop();
                    if (InheritanceParentsByUid.ContainsKey(uid))
                    {
                        InheritanceParentsByUid[uid].ForEach(u => uidsToCheck.Push(u));
                    }
                    if (ExtensionMethodUidsByTargetUid.Contains(uid))
                    {
                        var exCandiates = ExtensionMethodUidsByTargetUid[uid].Where(ex =>
                        {
                            if (string.IsNullOrEmpty(ex.Uid))
                            {
                                return false;
                            }
                            List<string> exMonikers = null;
                            if (ex.ParentType != null && ex.ParentType.Metadata.TryGetValue(OPSMetadata.Monikers, out object monikers))
                            {
                                exMonikers = monikers as List<string>;
                            }
                            return (exMonikers == null && typeMonikers == null) ||
                                   (exMonikers != null && typeMonikers != null && exMonikers.Intersect(typeMonikers).Any());
                        });

                        extensionMethods.AddRange(exCandiates.Select(ex => ex.Uid));
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
            if (_frameworks == null || _frameworks.DocIdToFrameworkDict.Count == 0)
            {
                return;
            }
            if (_monikerAssemblyMapping != null)
            {
                _frameworks.FrameworkAssembliesPurged = _frameworks.FrameworkAssemblies?.ToDictionary(
                    p => p.Key,
                    p => p.Value.Where(a => _monikerAssemblyMapping[p.Key].Contains(a.Key)).ToDictionary(purged => purged.Key, purged => purged.Value));
            }

            foreach (var ns in _nsList)
            {
                if (_frameworks.DocIdToFrameworkDict.ContainsKey(ns.Uid))
                {
                    MonikerizeItem(ns, _frameworks.DocIdToFrameworkDict[ns.Uid]);
                }
                foreach (var t in ns.Types)
                {
                    if (!string.IsNullOrEmpty(t.DocId) && _frameworks.DocIdToFrameworkDict.ContainsKey(t.DocId))
                    {
                        MonikerizeItem(t, _frameworks.DocIdToFrameworkDict[t.DocId]);
                    }
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members.Where(m => !string.IsNullOrEmpty(m.DocId)))
                        {
                            if (_frameworks.DocIdToFrameworkDict.ContainsKey(m.DocId))
                            {
                                MonikerizeItem(m, _frameworks.DocIdToFrameworkDict[m.DocId]);
                            }
                            else
                            {
                                OPSLogger.LogUserError(string.Format("Unable to find framework info for {0}", m.DocId), m.SourceFileLocalPath);
                            }
                        }
                    }
                    //special handling for monikers metadata
                    if (t.Overloads != null)
                    {
                        foreach (var ol in t.Overloads)
                        {
                            var monikers = t.Members.Where(m => m.Overload == ol.Uid && !string.IsNullOrEmpty(m.DocId))
                                .SelectMany(m => _frameworks.DocIdToFrameworkDict.ContainsKey(m.DocId) ? _frameworks.DocIdToFrameworkDict[m.DocId] : Enumerable.Empty<string>()).Distinct().ToList();
                            if (monikers?.Count > 0)
                            {
                                MonikerizeItem(ol, monikers);
                            }
                        }
                    }
                }
            }
        }

        private void MonikerizeItem(ReflectionItem item, List<string> monikers)
        {
            item.Metadata[OPSMetadata.Monikers] = monikers;
            if (_frameworks.FrameworkAssembliesPurged?.Count > 0 && item.AssemblyInfo != null)
            {
                var valuesPerMoniker = new Dictionary<string, List<AssemblyInfo>>();
                foreach (var moniker in monikers)
                {
                    var frameworkAssemblies = _frameworks.FrameworkAssembliesPurged[moniker];
                    var assemblies = item.AssemblyInfo.Where(
                        itemAsm => frameworkAssemblies.TryGetValue(itemAsm.Name, out var fxAsm) && fxAsm.Version == itemAsm.Version).ToList();
                    if (assemblies.Count == 0)
                    {
                        // due to https://github.com/mono/api-doc-tools/issues/400, sometimes the versions don't match
                        var fallbackAssemblies = item.AssemblyInfo.Where(
                            itemAsm => frameworkAssemblies.TryGetValue(itemAsm.Name, out var fxAsm)).ToList();
                        if (fallbackAssemblies != null)
                        {
                            valuesPerMoniker[moniker] = fallbackAssemblies;
                            OPSLogger.LogUserInfo($"{item.Uid}'s moniker {moniker} can't match any assembly by version, fallback to name matching.");
                        }
                        else
                        {
                            OPSLogger.LogUserWarning($"{item.Uid}'s moniker {moniker} can't match any assembly.");
                        }
                    }
                    else
                    {
                        
                    }
                    valuesPerMoniker[moniker] = assemblies;
                }
                item.VersionedAssemblyInfo = new VersionedProperty<AssemblyInfo>(valuesPerMoniker);
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
                || m.ItemType == ItemType.Operator
                || m.ItemType == ItemType.AttachedProperty)
                .ToList();
            if (methods?.Count() > 0)
            {
                Dictionary<string, Member> overloads = null;
                if (t.Overloads?.Count > 0)
                {
                    overloads = t.Overloads.Where(o => methods.Exists(m => m.Name == o.Name))
                        .ToDictionary(o => methods.First(m => m.Name == o.Name).GetOverloadId());
                }
                else
                {
                    overloads = new Dictionary<string, Member>();
                }
                foreach (var m in methods)
                {
                    string id = m.GetOverloadId();
                    if (!overloads.ContainsKey(id))
                    {
                        overloads.Add(id, new Member()
                        {
                            Name = m.Name,
                            Parent = t
                        });
                    }
                    var displayName = m.DisplayName;
                    if (displayName.Contains('('))
                    {
                        displayName = displayName.Substring(0, displayName.LastIndexOf('('));
                    }
                    if (displayName.Contains('<'))
                    {
                        if (!displayName.Contains('.') || displayName.LastIndexOf('<') > displayName.LastIndexOf('.'))
                        {
                            displayName = displayName.Substring(0, displayName.LastIndexOf('<'));
                        }
                    }
                    overloads[id].Id = id;
                    overloads[id].DisplayName = m.ItemType == ItemType.Constructor ? t.Name : displayName;
                    overloads[id].FullDisplayName = t.FullName + "." + overloads[id].DisplayName;
                    overloads[id].SourceFileLocalPath = m.SourceFileLocalPath;

                    if (overloads[id].AssemblyInfo == null)
                    {
                        overloads[id].AssemblyInfo = new List<AssemblyInfo>();
                    }
                    overloads[id].AssemblyInfo.AddRange(m.AssemblyInfo);

                    m.Overload = overloads[id].Uid;
                }
                if (overloads.Count > 0)
                {
                    foreach(var overload in overloads.Values)
                    {
                        overload.AssemblyInfo = overload.AssemblyInfo.Distinct().ToList();
                    }
                    t.Overloads = overloads.Values.ToList();
                }
            }

        }

        private void BuildAttributes()
        {
            foreach (var t in _tList)
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

        string[] attributePrefix = { "get: ", "set: ", "add: ", "remove: " };

        private void ResolveAttribute(ECMAAttribute attr)
        {
            var fqn = attr.Declaration;
            if (fqn.Contains("("))
            {
                fqn = fqn.Substring(0, fqn.IndexOf("("));
            }
            foreach (var prefix in attributePrefix)
            {
                if (fqn.StartsWith(prefix))
                {
                    fqn = fqn.Substring(prefix.Length);
                }
            }
            var nameWithSuffix = fqn + "Attribute";
            if (TypesByFullName.ContainsKey(nameWithSuffix) || !TypesByFullName.ContainsKey(fqn))
            {
                fqn = nameWithSuffix;
            }
            attr.TypeFullName = fqn;
            if (FilterStore?.AttributeFilters?.Count > 0)
            {
                foreach (var f in FilterStore.AttributeFilters)
                {
                    var result = TypesByFullName.ContainsKey(fqn) ? f.Filter(TypesByFullName[fqn]) : f.Filter(fqn);
                    if (result.HasValue)
                    {
                        attr.Visible = result.Value;
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
            if (t.ItemType == ItemType.Interface)
            {
                BuildInheritanceForInterface(t);
            }
            else
            {
                BuildInheritanceDefault(t);
            }
        }

        private void BuildInheritanceForInterface(Type t)
        {
            if (t.Interfaces?.Count > 0)
            {
                t.InheritedMembers = new Dictionary<string, string>();
                foreach (var f in t.Interfaces)
                {
                    var interfaceUid = f.ToOuterTypeUid();
                    AddInheritanceMapping(t.Uid, interfaceUid);

                    if (TypesByUid.TryGetValue(interfaceUid, out Type inter))
                    {
                        if (inter.Members != null)
                        {
                            foreach (var m in inter.Members)
                            {
                                if (m.Name != "Finalize" && m.ItemType != ItemType.Constructor && !(m.IsStatic.HasValue && m.IsStatic.Value))
                                {
                                    t.InheritedMembers[m.Id] = inter.Uid;
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

        private void BuildInheritanceDefault(Type t)
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

                if (t.ItemType == ItemType.Class && ! (t.IsStatic.HasValue && t.IsStatic.Value))
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
                                    if (m.Name != "Finalize" && m.ItemType != ItemType.Constructor && ! (m.IsStatic.HasValue && m.IsStatic.Value))
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
            if (t.Parameters != null && t.Docs?.Parameters != null)
            {
                foreach (var tp in t.Parameters)
                {
                    tp.Description = t.Docs.Parameters.ContainsKey(tp.Name) ? t.Docs.Parameters[tp.Name] : null;
                }
            }
            if (t.Members != null)
            {
                foreach (var m in t.Members)
                {
                    // comment out this code so we don't remove duplicated notes, for https://ceapex.visualstudio.com/Engineering/_workitems/edit/41762
                    //if (m.Docs?.AdditionalNotes != null && t.Docs?.AdditionalNotes != null)
                    //{
                    //    m.Docs.AdditionalNotes = m.Docs.AdditionalNotes.Where(p => !(t.Docs.AdditionalNotes.ContainsKey(p.Key) && t.Docs.AdditionalNotes[p.Key] == p.Value))
                    //        .ToDictionary(p => p.Key, p => p.Value);
                    //}
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
            if (t.ReturnValueType != null && t.Docs?.Returns != null)
            {
                t.ReturnValueType.Description = t.Docs.Returns;
            }
        }

        private void FindMissingAssemblyNamesAndVersions()
        {
            foreach (var t in _tList)
            {
                if (t.AssemblyInfo?.Count > 0 && t.Members?.Count > 0)
                {
                    foreach (var m in t.Members)
                    {
                        if (m.AssemblyInfo?.Count > 0)
                        {
                            foreach (var asm in m.AssemblyInfo)
                            {
                                if (string.IsNullOrEmpty(asm.Name) && asm.Version != null)
                                {
                                    var fallback = t.AssemblyInfo.FirstOrDefault(ta => ta.Version == asm.Version);
                                    asm.Name = fallback?.Name;
                                    OPSLogger.LogUserInfo($"AssemblyName fallback for {m.DocId} to {asm.Name}", m.SourceFileLocalPath);
                                }
                            }
                            // hack for https://github.com/mono/api-doc-tools/issues/399
                            var missingVersion = m.AssemblyInfo.Where(a => a.Version == null).ToList();
                            foreach(var asm in missingVersion)
                            {
                                var parentFallback = t.AssemblyInfo.Where(a => a.Name == asm.Name).ToList();
                                if (parentFallback.Count > 0)
                                {
                                    m.AssemblyInfo.Remove(asm);
                                    m.AssemblyInfo.AddRange(parentFallback);
                                    OPSLogger.LogUserInfo($"AssemblyVersion fallback for {m.DocId}, {asm.Name}", m.SourceFileLocalPath);
                                }
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
            else if (typeString != null && typeString.EndsWith("&"))
            {
                if (EcmaParser.TryParse("T:" + typeString.TrimEnd('&'), out desc))
                {
                    desc.DescModifier = EcmaDesc.Mod.Ref;
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
