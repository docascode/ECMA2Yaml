using Monodoc.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

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
        public Dictionary<string, List<VersionedString>> InheritanceParentsByUid { get; set; }
        public Dictionary<string, List<VersionedString>> InheritanceChildrenByUid { get; set; }
        public Dictionary<string, List<VersionedString>> ImplementationParentsByUid { get; set; }
        public Dictionary<string, List<VersionedString>> ImplementationChildrenByUid { get; set; }
        public Dictionary<string, Member> ExtensionMethodsByMemberDocId { get; set; }
        public ILookup<string, Member> ExtensionMethodUidsByTargetUid { get; set; }
        public FilterStore FilterStore { get; set; }
        public bool StrictMode { get; set; }
        public bool UWPMode { get; set; }

        private static Dictionary<string, EcmaDesc> typeDescriptorCache;

        private IEnumerable<Namespace> _nsList;
        private IEnumerable<Type> _tList;
        private FrameworkIndex _frameworks;
        private List<Member> _extensionMethods;
        private Dictionary<string, string> _monikerNugetMapping;
        private Dictionary<string, List<string>> _monikerAssemblyMapping;
        private Dictionary<string, List<string>> _assemblyMonikerMapping;

        public ECMAStore(IEnumerable<Namespace> nsList,
            FrameworkIndex frameworks,
            Dictionary<string, string> monikerNugetMapping = null,
            Dictionary<string, List<string>> monikerAssemblyMapping = null)
        {
            typeDescriptorCache = new Dictionary<string, EcmaDesc>();

            _nsList = nsList;
            _tList = nsList.SelectMany(ns => ns.Types).ToList();
            _frameworks = frameworks;
            _monikerNugetMapping = monikerNugetMapping;
            _monikerAssemblyMapping = monikerAssemblyMapping;

            InheritanceParentsByUid = new Dictionary<string, List<VersionedString>>();
            InheritanceChildrenByUid = new Dictionary<string, List<VersionedString>>();
            ImplementationParentsByUid = new Dictionary<string, List<VersionedString>>();
            ImplementationChildrenByUid = new Dictionary<string, List<VersionedString>>();
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
            }

            PopulateMonikers();

            foreach (var t in _tList)
            {
                FillInheritanceImplementationGraph(t);
            }

            foreach (var t in _tList)
            {
                BuildInheritance(t);
                BuildDocs(t);
            }

            FindMissingAssemblyNamesAndVersions();

            BuildAttributes();

            MonikerizeAssembly();

            BuildExtensionMethods();

            BuildOtherMetadata();
        }

        private void BuildDocIdDictionary()
        {
            ItemsByDocId = new Dictionary<string, ReflectionItem>();
            foreach (var item in TypesByUid.Values.Cast<ReflectionItem>()
                .Concat(MembersByUid.Values.Cast<ReflectionItem>()))
            {
                if (string.IsNullOrEmpty(item.DocId))
                {
                    OPSLogger.LogUserError(LogCode.ECMA2Yaml_DocId_IsNull, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_DocId_IsNull, item.Name), item.SourceFileLocalPath);
                }
                else if (ItemsByDocId.ContainsKey(item.DocId))
                {
                    OPSLogger.LogUserError(LogCode.ECMA2Yaml_DocId_Duplicated, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_DocId_Duplicated, item.DocId), item.SourceFileLocalPath);
                    OPSLogger.LogUserError(LogCode.ECMA2Yaml_DocId_Duplicated, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_DocId_Duplicated, item.DocId), ItemsByDocId[item.DocId].SourceFileLocalPath);
                }
                else
                {
                    ItemsByDocId.Add(item.DocId, item);
                }
            }
        }

        private void BuildUniqueMembers()
        {
            var allMembers = _tList.Where(t => t.Members != null).SelectMany(t => t.Members).ToList();
            var groups = allMembers.GroupBy(m => m.Uid).Where(g => g.Count() > 1).ToList();
            if (groups.Count > 0)
            {
                foreach (var group in groups)
                {
                    foreach (var member in group)
                    {
                        OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_MemberNameAndSignature_NotUnique, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_MemberNameAndSignature_NotUnique, member.Name), member.SourceFileLocalPath);
                    }
                }
            }

            MembersByUid = new Dictionary<string, Member>();
            var typesToLower = TypesByUid.ToDictionary(p => p.Key.ToLower(), p => p.Value);
            foreach (var member in allMembers)
            {
                if (typesToLower.ContainsKey(member.Uid.ToLower()) || MembersByUid.ContainsKey(member.Uid))
                {
                    member.Id = member.Id + "_" + member.ItemType.ToString().Substring(0, 1).ToLower();
                }
                MembersByUid[member.Uid] = member;
            }
        }

        public bool TryGetTypeByFullName(string fullName, out Type type)
        {
            if (fullName.IndexOf('+') > 0)
            {
                return TypesByFullName.TryGetValue(fullName.Replace('+', '.'), out type);
            }
            else
            {
                return TypesByFullName.TryGetValue(fullName, out type);
            }
        }

        public void TranslateSourceLocation(
            string sourcePathRoot,
            string gitRepoUrl,
            string gitRepoBranch,
            string publicGitRepoUrl, 
            string publicGitBranch)
        {
            sourcePathRoot = System.IO.Path.GetFullPath(sourcePathRoot);
            if (!sourcePathRoot.EndsWith("\\"))
            {
                sourcePathRoot += "\\";
            }

            var gitUrlPattern = GetGitUrlGenerator(gitRepoUrl, gitRepoBranch);
            var publicGitUrlPattern = GetGitUrlGenerator(publicGitRepoUrl, publicGitBranch);

            foreach (var ns in _nsList)
            {
                TranslateSourceLocation(ns, sourcePathRoot, gitUrlPattern, publicGitUrlPattern);
                HandleContentSourceMeta(ns);
                foreach (var t in ns.Types)
                {
                    TranslateSourceLocation(t, sourcePathRoot, gitUrlPattern, publicGitUrlPattern);
                    HandleContentSourceMeta(t);
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members)
                        {
                            TranslateSourceLocation(m, sourcePathRoot, gitUrlPattern, publicGitUrlPattern);
                            HandleContentSourceMeta(m);
                        }
                        if (t.Overloads != null)
                        {
                            foreach (var o in t.Overloads)
                            {
                                TranslateSourceLocation(o, sourcePathRoot, gitUrlPattern, publicGitUrlPattern);
                                HandleContentSourceMeta(o);
                            }
                        }
                    }
                }
            }

            Func<string, string> GetGitUrlGenerator(string gitUrl, string gitBranch)
            {
                bool isVSTS = gitUrl.Contains("visualstudio.com");
                if (isVSTS)
                {
                    string pattern = gitUrl + "?path={0}&version=GB" + gitBranch;
                    return xmlPath => string.Format(pattern, WebUtility.UrlEncode(xmlPath));
                }
                else
                {
                    string pattern = gitUrl + "/blob/" + gitBranch + "{0}";
                    return xmlPath => string.Format(pattern, xmlPath);
                }
            }

            void HandleContentSourceMeta(ReflectionItem item)
            {
                if (item.Metadata.TryGetValue("contentSourcePath", out object val) && val != null)
                {
                    var mdPath = val.ToString().Replace("\\", "/");
                    mdPath = mdPath.StartsWith("/") ? mdPath : ("/" + mdPath);
                    item.SourceDetail = new GitSourceDetail()
                    {
                        Path = mdPath,
                        RepoBranch = publicGitBranch,
                        RepoUrl = publicGitRepoUrl
                    };
                    item.Metadata.Remove("contentSourcePath");
                }
            }
        }

        /// <summary>
        /// reference doc: https://review.docs.microsoft.com/en-us/engineering/projects/ops/edit-button?branch=master
        /// content_git_url: the url that is used in live page edit button
        /// original_content_git_url: the url of the file that is used to really publish the page. Also used in review page edit button.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sourcePathRoot"></param>
        /// <param name="gitUrlPattern"></param>
        /// <param name="publicGitUrlPattern"></param>
        /// <param name="vstsRepo"></param>
        private void TranslateSourceLocation(
            ReflectionItem item,
            string sourcePathRoot,
            Func<string, string> gitUrlPattern,
            Func<string, string> publicGitUrlPattern)
        {
            if (!string.IsNullOrEmpty(item.SourceFileLocalPath)
                && item.SourceFileLocalPath.StartsWith(sourcePathRoot)
                && !item.Metadata.ContainsKey(OPSMetadata.ContentUrl))
            {
                string xmlPath = item.SourceFileLocalPath.Replace(sourcePathRoot, "/").Replace("\\", "/");
                
                string contentGitUrl = publicGitUrlPattern(xmlPath);
                item.Metadata[OPSMetadata.ContentUrl] = contentGitUrl;

                string originalContentGitUrl = gitUrlPattern(xmlPath);
                item.Metadata[OPSMetadata.OriginalContentUrl] = originalContentGitUrl;
                item.Metadata[OPSMetadata.RefSkeletionUrl] = originalContentGitUrl;
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
                    ns.Metadata[OPSMetadata.AltCompliant] = ns.Docs?.AltCompliant.ResolveCommentId(this)?.Uid;
                }
                if (nsInternalOnly)
                {
                    ns.Metadata[OPSMetadata.InternalOnly] = nsInternalOnly;
                }
                if (_monikerNugetMapping != null && ns.Monikers != null)
                {
                    var monikers = ns.Monikers as IEnumerable<string>;
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
                        t.Metadata[OPSMetadata.AltCompliant] = t.Docs?.AltCompliant.ResolveCommentId(this)?.Uid;
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
                                m.Metadata[OPSMetadata.AltCompliant] = m.Docs?.AltCompliant.ResolveCommentId(this)?.Uid;
                            }
                            if (mInternalOnly)
                            {
                                m.Metadata[OPSMetadata.InternalOnly] = mInternalOnly;
                            }
                            if (m.ExtendedMetadata == null || m.ExtendedMetadata.Count == 0)
                            {
                                m.ExtendedMetadata = t.ExtendedMetadata;
                            }
                            AddAdditionalNotes(m);
                        }
                    }
                    if (t.Overloads != null)
                    {
                        foreach(var ol in t.Overloads)
                        {
                            if (ol.ExtendedMetadata == null || ol.ExtendedMetadata.Count == 0)
                            {
                                ol.ExtendedMetadata = t.ExtendedMetadata;
                            }
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
                            if (item.ItemType == ItemType.Interface
                                || item.Parent?.ItemType == ItemType.Interface
                                || item.Signatures.IsAbstract == true)
                            {
                                notes.Implementer = val;
                            }
                            else if (item.ItemType == ItemType.Class || item.Parent?.ItemType == ItemType.Class)
                            {
                                notes.Inheritor = val;
                            }
                            break;
                        default:
                            OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_NotesType_UnKnown, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_NotesType_UnKnown, note.Key), item.SourceFileLocalPath);
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
            _extensionMethods = new List<Member>();
            foreach (var m in MembersByUid.Values)
            {
                if (!string.IsNullOrEmpty(m.DocId))
                {
                    var thisParam = m.Parameters?.FirstOrDefault(p =>  p.RefType == "this");
                    if (m.Parameters != null && thisParam != null)
                    {
                        if (m.Parent != null && m.Parent.Signatures.IsStatic)
                        {
                            var targetDocId = thisParam.Type;

                            // Temp fix bug: 106581- C# extension methods not showing up in class reference docs
                            // Next step, transfer special char like "+" when loading xml data, need add more case to cover this change.
                            if (targetDocId.Contains("+"))
                            {
                                targetDocId = targetDocId.Replace("+",".");
                            }

                            m.IsExtensionMethod = true;
                            if (TypesByFullName.TryGetValue(targetDocId, out Type type))
                            {
                                m.TargetDocId = type.DocId;
                                _extensionMethods.Add(m);
                            }
                        }
                    }
                }
            }

            if (_extensionMethods.Count == 0)
            {
                return;
            }

            ExtensionMethodsByMemberDocId = _extensionMethods.ToDictionary(ex => ex.DocId);
            ExtensionMethodUidsByTargetUid = _extensionMethods.ToLookup(ex => ex.TargetDocId.Replace("T:", ""));
            foreach (var ex in _extensionMethods.Where(ex => ex.Uid == null))
            {
                OPSLogger.LogUserInfo(string.Format("ExtensionMethod {0} not found in its type {1}", ex.DocId, ex.Parent.Name), "index.xml");
            }

            foreach (var t in _tList)
            {
                var exMethodsFromBaseType = CheckAvailableExtensionMethods(t, InheritanceParentsByUid);
                //var exMethodsFromInterface = CheckAvailableExtensionMethods(t, ImplementationParentsByUid);
                //var allExMethods = exMethodsFromBaseType.MergeWith(exMethodsFromInterface);
                if (exMethodsFromBaseType.Count > 0)
                {
                    t.ExtensionMethods = exMethodsFromBaseType.Distinct().ToList();
                    t.ExtensionMethods.Sort();
                }
            }
        }

        private List<string> CheckAvailableExtensionMethods(Type t, Dictionary<string, List<VersionedString>> parentDict)
        {
            List<string> extensionMethods = new List<string>();
            Stack<string> uidsToCheck = new Stack<string>();
            uidsToCheck.Push(t.Uid);
            while (uidsToCheck.Count > 0)
            {
                var uid = uidsToCheck.Pop();
                if (parentDict.ContainsKey(uid))
                {
                    parentDict[uid].ForEach(u => uidsToCheck.Push(u.Value));
                }
                var exCandiates = GetExtensionMethodCandidatesForType(uid);
                if (exCandiates != null)
                {
                    extensionMethods.AddRange(exCandiates);
                }
                if (TypesByUid.TryGetValue(uid, out var type) && type.Interfaces?.Count > 0)
                {
                    foreach(var f in type.Interfaces)
                    {
                        exCandiates = GetExtensionMethodCandidatesForType(f.ToOuterTypeUid());
                        if (exCandiates != null)
                        {
                            extensionMethods.AddRange(exCandiates);
                        }
                    }
                }
            }
            return extensionMethods;

            List<string> GetExtensionMethodCandidatesForType(string uid)
            {
                if (ExtensionMethodUidsByTargetUid.Contains(uid))
                {
                    var exCandiates = ExtensionMethodUidsByTargetUid[uid].Where(ex =>
                    {
                        if (string.IsNullOrEmpty(ex.Uid))
                        {
                            return false;
                        }
                        HashSet<string> exMonikers = ex.Parent?.Monikers;
                        return (exMonikers == null && t.Monikers == null) ||
                               (exMonikers != null && t.Monikers != null && exMonikers.Intersect(t.Monikers).Any());
                    });

                    return exCandiates.Select(ex => ex.Uid).ToList();
                }
                return null;
            }
        }

        private void PopulateMonikers()
        {
            if (_frameworks == null || _frameworks.DocIdToFrameworkDict.Count == 0)
            {
                return;
            }
            if (_monikerAssemblyMapping != null && _monikerAssemblyMapping.Count > 0)
            {
                _frameworks.FrameworkAssemblies = _frameworks.FrameworkAssemblies?.ToDictionary(
                    p => p.Key,
                    p => p.Value.Where(a => _monikerAssemblyMapping[p.Key].Contains(a.Key)).ToDictionary(purged => purged.Key, purged => purged.Value));
            }

            var allMonikers = _frameworks.FrameworkAssemblies.Keys.ToHashSet();
            foreach (var ns in _nsList)
            {
                if (_frameworks.DocIdToFrameworkDict.ContainsKey(ns.Uid))
                {
                    ns.Monikers = new HashSet<string>(_frameworks.DocIdToFrameworkDict[ns.Uid]);
                }
                foreach (var t in ns.Types)
                {
                    if (!string.IsNullOrEmpty(t.DocId) && _frameworks.DocIdToFrameworkDict.ContainsKey(t.DocId))
                    {
                        t.Monikers = new HashSet<string>(_frameworks.DocIdToFrameworkDict[t.DocId]);
                        if (t.BaseTypes?.Count > 0)
                        {
                            //specify monikers for easier calculation
                            var remainingMonikers = new HashSet<string>(allMonikers);
                            foreach (var bt in t.BaseTypes.Where(b => b.Monikers != null))
                            {
                                remainingMonikers.ExceptWith(bt.Monikers);
                            }
                            foreach (var bt in t.BaseTypes.Where(b => b.Monikers == null))
                            {
                                bt.Monikers = remainingMonikers;
                            }
                        }
                    }
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members.Where(m => !string.IsNullOrEmpty(m.DocId)))
                        {
                            if (_frameworks.DocIdToFrameworkDict.ContainsKey(m.DocId))
                            {
                                m.Monikers = new HashSet<string>(_frameworks.DocIdToFrameworkDict[m.DocId]);
                            }
                            else
                            {
                                OPSLogger.LogUserError(LogCode.ECMA2Yaml_Framework_NotFound, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_Framework_NotFound, m.DocId), m.SourceFileLocalPath);
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
                                ol.Monikers = new HashSet<string>(monikers);
                            }
                        }
                    }
                }
            }
        }

        private void MonikerizeAssembly()
        {
            foreach (var ns in _nsList)
            {
                foreach (var t in ns.Types)
                {
                    if (t.Monikers != null)
                    {
                        MonikerizeAssembly(t, t.Monikers);
                    }
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members.Where(m => !string.IsNullOrEmpty(m.DocId)))
                        {
                            if (m.Monikers != null)
                            {
                                MonikerizeAssembly(m, m.Monikers);
                            }
                        }
                    }
                    //special handling for monikers metadata
                    if (t.Overloads != null)
                    {
                        foreach (var ol in t.Overloads)
                        {
                            if (ol.Monikers != null)
                            {
                                MonikerizeAssembly(ol, ol.Monikers);
                            }
                        }
                    }
                }
            }
        }

        private void MonikerizeAssembly(ReflectionItem item, IEnumerable<string> monikers)
        {
            if (_frameworks.FrameworkAssemblies?.Count > 0 && item.AssemblyInfo != null)
            {
                var valuesPerMoniker = new Dictionary<string, List<AssemblyInfo>>();
                foreach (var moniker in monikers)
                {
                    var frameworkAssemblies = _frameworks.FrameworkAssemblies[moniker];
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
                            OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_UidAssembly_NotMatched, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_UidAssembly_NotMatched, item.Uid, moniker));
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
                if (t.BaseTypes != null)
                {
                    t.BaseTypes.ForEach(bt => bt.Build(this));
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
                            Parent = t,
                            ItemType = m.ItemType
                        });
                    }
                    overloads[id].Id = id;
                    overloads[id].ItemType = m.ItemType;
                    overloads[id].DisplayName = m.ItemType == ItemType.Constructor ? t.Name : TrimDisplayName(m.DisplayName);
                    overloads[id].FullDisplayName = overloads[id].FullDisplayName ?? TrimDisplayName(m.FullDisplayName);
                    overloads[id].SourceFileLocalPath = m.SourceFileLocalPath;

                    if (overloads[id].Modifiers == null)
                    {
                        overloads[id].Modifiers = new SortedList<string, List<string>>();
                    }
                    foreach(var pair in m.Modifiers)
                    {
                        if (overloads[id].Modifiers.ContainsKey(pair.Key))
                        {
                            overloads[id].Modifiers[pair.Key].AddRange(pair.Value);
                        }
                        else
                        {
                            overloads[id].Modifiers[pair.Key] = new List<string>(pair.Value);
                        }
                    }

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
                        foreach (var lang in overload.Modifiers.Keys.ToList())
                        {
                            overload.Modifiers[lang] = overload.Modifiers[lang].Distinct().ToList();
                        }
                        overload.AssemblyInfo = overload.AssemblyInfo.Distinct().ToList();
                        ItemsByDocId[overload.CommentId] = overload;
                    }
                    t.Overloads = overloads.Values.ToList();
                }
            }

            string TrimDisplayName(string displayName)
            {
                if (displayName.Contains('('))
                {
                    displayName = displayName.Substring(0, displayName.LastIndexOf('('));
                }
                //if (displayName.Contains('['))
                //{
                //    displayName = displayName.Substring(0, displayName.LastIndexOf('['));
                //}
                if (displayName.Contains('<'))
                {
                    if (!displayName.Contains('.') || displayName.LastIndexOf('<') > displayName.LastIndexOf('.'))
                    {
                        displayName = displayName.Substring(0, displayName.LastIndexOf('<'));
                    }
                }
                return displayName;
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

        readonly string[] attributePrefix = { "get: ", "set: ", "add: ", "remove: " };

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

        private void AddInheritanceMapping(string childUid, string parentUid, HashSet<string> monikers = null)
        {
            if (!InheritanceParentsByUid.ContainsKey(childUid))
            {
                InheritanceParentsByUid.Add(childUid, new List<VersionedString>());
            }
            InheritanceParentsByUid[childUid].Add(new VersionedString() { Value = parentUid, Monikers = monikers });

            if (!InheritanceChildrenByUid.ContainsKey(parentUid))
            {
                InheritanceChildrenByUid.Add(parentUid, new List<VersionedString>());
            }
            InheritanceChildrenByUid[parentUid].Add(new VersionedString() { Value = childUid, Monikers = monikers });
        }

        private void AddImplementMapping(string childUid, string parentUid, HashSet<string> monikers = null)
        {
            if (!ImplementationParentsByUid.ContainsKey(childUid))
            {
                ImplementationParentsByUid.Add(childUid, new List<VersionedString>());
            }
            ImplementationParentsByUid[childUid].Add(new VersionedString() { Value = parentUid, Monikers = monikers });

            if (!ImplementationChildrenByUid.ContainsKey(parentUid))
            {
                ImplementationChildrenByUid.Add(parentUid, new List<VersionedString>());
            }
            ImplementationChildrenByUid[parentUid].Add(new VersionedString() { Value = childUid, Monikers = monikers });
        }

        private void FillInheritanceImplementationGraph(Type t)
        {
            if (t.Interfaces?.Count > 0)
            {
                foreach (var f in t.Interfaces)
                {
                    AddImplementMapping(t.Uid, f.ToOuterTypeUid());
                }
            }
            if (t.BaseTypes != null)
            {
                foreach(var bt in t.BaseTypes)
                {
                    if (bt.Uid != t.Uid)
                    {
                        AddInheritanceMapping(t.Uid, bt.Uid, bt.Monikers);
                    }
                }
            }
        }

        public List<VersionedValue<List<string>>> BuildInheritanceChain(string uid)
        {
            if (!TypesByUid.TryGetValue(uid, out Type t))
            {
                return null;
            }
            if (t.InheritanceChains != null)
            {
                return t.InheritanceChains; //already calculated, return directly
            }
            else if (InheritanceParentsByUid.TryGetValue(uid, out var parents))
            {
                var inheritanceChains = new List<VersionedValue<List<string>>>();
                foreach (var parent in parents)
                {
                    var grandParents = BuildInheritanceChain(parent.Value);
                    if (grandParents == null)
                    {
                        inheritanceChains.Add(new VersionedValue<List<string>>(parent.Monikers, new List<string>() { parent.Value }));
                    }
                    else
                    {
                        foreach(var grandParentChain in grandParents)
                        {
                            if (parent.Monikers.Overlaps(grandParentChain.Monikers))
                            {
                                var commonMonikers = new HashSet<string>(parent.Monikers.Intersect(grandParentChain.Monikers));
                                if (commonMonikers.Overlaps(t.Monikers))
                                {
                                    var chain = new List<string>(grandParentChain.Value);
                                    chain.Add(parent.Value);
                                    inheritanceChains.Add(new VersionedValue<List<string>>(commonMonikers, chain));
                                }
                            }
                        }
                    }
                }
                t.InheritanceChains = inheritanceChains;
                return inheritanceChains;
            }
            else
            {
                t.InheritanceChains = null;
                return null;
            }
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

                    if (TypesByUid.TryGetValue(interfaceUid, out Type inter))
                    {
                        if (inter.Members != null)
                        {
                            foreach (var m in inter.Members)
                            {
                                if (m.Name != "Finalize" && m.ItemType != ItemType.Constructor && !m.Signatures.IsStatic)
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
            if (t.BaseTypes != null)
            {
                t.InheritanceChains = BuildInheritanceChain(t.Uid);

                if (t.ItemType == ItemType.Class && !t.Signatures.IsStatic)
                {
                    t.InheritedMembers = new Dictionary<string, string>();
                    foreach(var inheritanceChain in t.InheritanceChains)
                    {
                        foreach (var btUid in inheritanceChain.Value)
                        {
                            if (TypesByUid.ContainsKey(btUid))
                            {
                                var bt = TypesByUid[btUid];
                                if (bt.Members != null)
                                {
                                    foreach (var m in bt.Members)
                                    {
                                        if (m.Name != "Finalize"
                                            && m.ItemType != ItemType.Constructor
                                            && m.ItemType != ItemType.AttachedProperty
                                            && m.ItemType != ItemType.AttachedEvent
                                            && !m.Signatures.IsStatic)
                                        {
                                            t.InheritedMembers[m.Id] = bt.Uid;
                                        }
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
                foreach (var ttp in t.TypeParameters)
                {
                    if (t.Docs.TypeParameters.TryGetValue(ttp.Name, out var ttpDesc))
                    {
                        ttp.Description = ttpDesc;
                    }
                }
            }
            if (t.Parameters != null && t.Docs?.Parameters != null)
            {
                foreach (var tp in t.Parameters)
                {
                    if (t.Docs.Parameters.TryGetValue(tp.Name, out var tpDesc))
                    {
                        tp.Description = tpDesc;
                    }
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
                            if (m.Docs.TypeParameters.TryGetValue(mtp.Name, out var mtpDesc))
                            {
                                mtp.Description = mtpDesc;
                            }
                        }
                    }
                    if (m.Parameters != null && m.Docs?.Parameters != null)
                    {
                        foreach (var mp in m.Parameters)
                        {
                            if (m.Docs.Parameters.TryGetValue(mp.Name, out var mpDesc))
                            {
                                mp.Description = mpDesc;
                            }
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
                                OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_ExceptionTypeNotFound, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_ExceptionTypeNotFound, ex.Uid), m.SourceFileLocalPath);
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
            EcmaDesc desc;
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

        public FrameworkIndex GetFrameworkIndex()
        {
            return _frameworks;
        }
    }
}
