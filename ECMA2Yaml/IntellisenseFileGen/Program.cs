using ECMA2Yaml;
using IntellisenseFileGen.Models;
using Microsoft.OpenPublishing.FileAbstractLayer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntellisenseFileGen
{
    class Program
    {
        static string _xmlDataFolder = @"G:\SourceCode\DevCode\dotnet-api-docs\xml";
        static string _rootFolder = @"G:\SourceCode\DevCode\dotnet-api-docs";
        static string _outFolder = @"G:\ECMA2Yaml-output\GenerateIntellisense\_intellisense";
        static Dictionary<string, string> _replaceStringDic = new Dictionary<string, string>() {
            { "\\\"","\"" },
            { "\\*","*" },
            { "\\\\","\\" },
            { "\\#","#" },
            { "\\_","_" },
        };
        private static string[] _ignoreTags = new string[] { "sup", "b", "csee", "br" };

        static void Main(string[] args)
        {
            var opt = new CommandLineOptions();
            if (opt.Parse(args))
            {
                _xmlDataFolder = Path.Combine(opt.DataRootPath, "xml");
                _outFolder = opt.OutFolder;
            }

            if (string.IsNullOrEmpty(_xmlDataFolder))
            {
                // TODO: log error

                return;
            }
            if (!Directory.Exists(_xmlDataFolder))
            {
                // TODO: log error
                return;
            }

            try
            {
                Start();
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
                // TODO: log error
            }
        }

        public static void Start()
        {
            var fileAccessor = new FileAccessor(_xmlDataFolder);
            ECMALoader loader = new ECMALoader(fileAccessor);
            var store = loader.LoadFolder("");
            if (store == null)
            {
                return;
            }
            store.Build();
            //store.ItemsByDocId.ToDictionary(p => p.Key, p.Value.CommentId);
            var typeList = LoadTypes(store.ItemsByDocId);
            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            var frameworks = store.GetFrameworkIndex();
            frameworks.FrameworkAssembliesPurged.Keys.ToList().ForEach(fw =>
            {
                string outPutFolder = Path.Combine(_outFolder, fw);

                var fwAssemblyList = frameworks.FrameworkAssembliesPurged[fw];
                var ass_Type_Mem_OfFw = frameworks.DocIdToFrameworkDict.Where(p => p.Value != null && p.Value.Contains(fw)).Select(p => p.Key).ToList();
                var fwTypeDocIdList = ass_Type_Mem_OfFw.Where(p => p.Contains("T:")).ToHashSet();
                var fwMemberDocIdList = ass_Type_Mem_OfFw.Where(p => p.Contains("M:") || p.Contains("P:") || p.Contains("F:") || p.Contains("E:")).ToHashSet();

                Parallel.ForEach(fwAssemblyList, opt, assembly =>
                {
                    string assemblyInfoStr = string.Format("{0}-{1}", assembly.Value.Name, assembly.Value.Version);
                    var assemblyTypes = typeList.Where(t =>
                    {
                        return t.AssemblyInfos.Exists(p => { return p == assemblyInfoStr; });
                    }).ToList();

                    // Order by xml
                    var selectedAssemblyTypes = assemblyTypes.Where(p => { return fwTypeDocIdList.Contains(p.DocId); });
                    if (selectedAssemblyTypes != null && selectedAssemblyTypes.Count() > 0)
                    {
                        XDocument intelligenceDoc = new XDocument(new XDeclaration("1.0", "utf-8", null));
                        var docEle = new XElement("doc");
                        var assemblyEle = new XElement("assembly");
                        var membersEle = new XElement("members");
                        docEle.Add(assemblyEle);
                        docEle.Add(membersEle);
                        intelligenceDoc.Add(docEle);
                        assemblyEle.SetElementValue("name", assembly.Value.Name);

                        selectedAssemblyTypes.OrderBy(p => p.DocId).ToList().ForEach(tt =>
                        {
                            membersEle.Add(tt.Docs);
                            if (tt.Members != null && tt.Members.Count() > 0)
                            {
                                if (tt.Members != null && tt.Members.Count() > 0)
                                {
                                    tt.Members.OrderBy(p => p.DocId).ToList().ForEach(m =>
                                    {
                                        if (fwMemberDocIdList.Contains(m.DocId) /*&& m.AssemblyInfos.Exists(p => { return p == assemblyInfoStr; })*/)
                                        {
                                            membersEle.Add(m.Docs);
                                        }
                                    });
                                }
                            }
                        });
                        if (membersEle.HasElements)
                        {
                            if (!Directory.Exists(outPutFolder))
                            {
                                Directory.CreateDirectory(outPutFolder);
                            }
                            intelligenceDoc.Save(Path.Combine(outPutFolder, assembly.Value.Name + ".xml"));
                            WriteLine($"Done generate {fw}.{assembly.Value.Name} intellisense files.");
                        }
                    }
                });
            });

            WriteLine($"All intellisense files done.");
        }

        /// <summary>
        /// Load Assemblies of every moniker
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, List<string>> LoadMonikerAssemblyList()
        {
            string monikerAssemblyMappingFile = $"{_xmlDataFolder}\\_moniker2Assembly.json";
            var monikerAssemblyList = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(monikerAssemblyMappingFile));

            return monikerAssemblyList;
        }

        /// <summary>
        /// Load Frameworks info, include assemblies
        /// </summary>
        /// <returns></returns>
        public static FrameworkIndex LoadFrameworks()
        {
            string frameworkIndexFolder = $"{_xmlDataFolder}\\FrameworksIndex";
            var frameworkIndexFileList = GetFiles(frameworkIndexFolder, "*.xml");

            FrameworkIndex frameworkIndex = new FrameworkIndex()
            {
                NamespaceDocIdsDict = new Dictionary<string, List<FrameworkDocIdInfo>>(),
                FrameworkAssemblies = new Dictionary<string, List<AssemblyInfo>>()
            };

            foreach (var fwFile in frameworkIndexFileList)
            {
                XDocument fxDoc = XDocument.Load(fwFile.FullName);
                var fxName = fxDoc.Root.Attribute("Name").Value;
                frameworkIndex.NamespaceDocIdsDict[fxName] = new List<FrameworkDocIdInfo>();
                foreach (var nsElement in fxDoc.Root.Elements("Namespace"))
                {
                    var ns = nsElement.Attribute("Name").Value;
                    foreach (var tElement in nsElement.Elements("Type"))
                    {
                        var t = tElement.Attribute("Id").Value;
                        frameworkIndex.NamespaceDocIdsDict[fxName].Add(new FrameworkDocIdInfo() { DocId = t, Namespace = ns });
                        foreach (var mElement in tElement.Elements("Member"))
                        {
                            var m = mElement.Attribute("Id").Value;
                            frameworkIndex.NamespaceDocIdsDict[fxName].Add(new FrameworkDocIdInfo() { DocId = m, Namespace = ns, ParentDocId = t });
                        }
                    }
                }

                var assemblyNodes = fxDoc.Root.Element("Assemblies")?.Elements("Assembly")?.Select(ele => new AssemblyInfo()
                {
                    Name = ele.Attribute("Name")?.Value,
                    Version = ele.Attribute("Version")?.Value,
                }).ToList();

                if (assemblyNodes != null)
                {
                    frameworkIndex.FrameworkAssemblies.Add(fxName, assemblyNodes);
                }
            }
            return frameworkIndex;
        }

        /// <summary>
        /// Load all xml file and convert to List<Type>
        /// </summary>
        /// <returns></returns>
        public static List<Models.Type> LoadTypes(Dictionary<string, ECMA2Yaml.Models.ReflectionItem> ItemsByDocId)
        {
            var typeFileList = GetFiles(_xmlDataFolder, "*.xml");
            List<Models.Type> typeList = new List<Models.Type>();
            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.ForEach(typeFileList, opt, typeFile =>
            {
                XDocument xmlDoc = XDocument.Load(typeFile.FullName);

                if (xmlDoc.Root.Name.LocalName == "Type")
                {
                    Models.Type t = ConvertToType(xmlDoc, ItemsByDocId);
                    if (t != null)
                    {
                        typeList.Add(t);
                    }
                }
            });

            return typeList;
        }

        /// <summary>
        /// Convert xml node(<Type></Type>) to Type object
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        private static Models.Type ConvertToType(XDocument xmlDoc, Dictionary<string, ECMA2Yaml.Models.ReflectionItem> ItemsByDocId)
        {
            Models.Type t = new Models.Type();

            var typeDocIdEle = xmlDoc.Root.Elements("TypeSignature")?.Where(p => p.Attribute("Language").Value == "DocId")?.FirstOrDefault();
            if (typeDocIdEle != null)
            {
                t.DocId = typeDocIdEle.Attribute("Value").Value;
            }

            var docsEle = new XElement("member");
            var typeSummaryEle = xmlDoc.Root.Element("Docs")?.Element("summary");
            var paramEles = xmlDoc.Root.Element("Docs")?.Elements("param");
            var typeparamEles = xmlDoc.Root.Element("Docs")?.Elements("typeparam");
            SpecialProcessElement(typeSummaryEle);
            if (!string.IsNullOrEmpty(typeSummaryEle?.Value))
            {
                docsEle.Add(typeSummaryEle);
            }
            BatchSpecialProcess(paramEles);
            BatchSpecialProcess(typeparamEles);

            // For some Uid, need to escape, the escaped uid is [CommentId]
            if (ItemsByDocId.ContainsKey(t.DocId))
            {
                docsEle.SetAttributeValue("name", ItemsByDocId[t.DocId].CommentId);
            }
            else
            {
                docsEle.SetAttributeValue("name", t.DocId);
            }
            docsEle.Add(paramEles);
            docsEle.Add(typeparamEles);
            t.Docs = docsEle;

            var AssemblyInfoEleList = xmlDoc.Root.Elements("AssemblyInfo");
            if (AssemblyInfoEleList != null)
            {
                t.AssemblyInfos = new List<string>();
                AssemblyInfoEleList.ToList().ForEach(assemblyInfoEle =>
                {
                    string assemblyName = assemblyInfoEle.Element("AssemblyName")?.Value;
                    var versionEles = assemblyInfoEle.Elements("AssemblyVersion");
                    if (versionEles != null && versionEles.Count() > 0)
                    {
                        versionEles.ToList().ForEach(assVersion =>
                        {
                            t.AssemblyInfos.Add(string.Format("{0}-{1}", assemblyName, assVersion.Value));
                        });
                    }
                });
            }

            if (t.AssemblyInfos.Count() == 0)
            {
                return null;
            }

            var memberEleList = xmlDoc.Root.Element("Members")?.Elements("Member");
            if (memberEleList != null)
            {
                t.Members = new List<Member>();
                memberEleList.ToList().ForEach(memberEle =>
                {
                    var m = ConvertToMember(memberEle, ItemsByDocId);
                    if (m != null)
                    {
                        t.Members.Add(m);
                    }
                });
            }

            return t;
        }

        /// <summary>
        /// Convert xml node(//Members/Member) to Member object
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private static Member ConvertToMember(XElement member, Dictionary<string, ECMA2Yaml.Models.ReflectionItem> ItemsByDocId)
        {
            var m = new Member();
            var memberDocIdEle = member.Elements("MemberSignature")?.Where(p => p.Attribute("Language").Value == "DocId").First();
            if (memberDocIdEle != null)
            {
                m.DocId = memberDocIdEle.Attribute("Value").Value;
            }
            SpecialProcessDuplicateParameters(member);

            var memberSummaryEle = member.Element("Docs")?.Element("summary");
            var paramEles = member.Element("Docs")?.Elements("param");
            var typeparamEles = member.Element("Docs")?.Elements("typeparam");
            var exceptionEles = member.Element("Docs")?.Elements("exception");

            var docsEle = new XElement("member");

            string realDocId = m.DocId;
            if (ItemsByDocId.ContainsKey(m.DocId))
            {
                string commentId = ItemsByDocId[m.DocId].CommentId;
                if (commentId != m.DocId)
                {
                    realDocId = commentId;
                }
            }

            docsEle.SetAttributeValue("name", SpecialProcessDocId(realDocId));

            SpecialProcessElement(memberSummaryEle);
            if (!string.IsNullOrEmpty(memberSummaryEle?.Value))
            {
                docsEle.Add(memberSummaryEle);
            }

            BatchSpecialProcess(paramEles);
            var withChildParaList = paramEles.Where(p => p.HasElements || !string.IsNullOrEmpty(p.Value)).ToList();
            if (withChildParaList != null && withChildParaList.Count > 0)
            {
                docsEle.Add(withChildParaList);
            }

            BatchSpecialProcess(typeparamEles);
            var withChildtypeParaList = typeparamEles.Where(p => p.HasElements || !string.IsNullOrEmpty(p.Value)).ToList();
            if (withChildtypeParaList != null && withChildtypeParaList.Count > 0)
            {
                docsEle.Add(withChildtypeParaList);
            }

            if (member.Element("Docs")?.Element("returns") != null)
            {
                if (member.Element("Docs")?.Element("returns").Value != "To be added.")
                {
                    var returnEle = member.Element("Docs")?.Element("returns");
                    SpecialProcessElement(returnEle);
                    docsEle.Add(returnEle);
                }
            }
            else if (member.Element("Docs")?.Element("value") != null)
            {
                if (member.Element("Docs")?.Element("value").Value != "To be added.")
                {
                    var child = member.Element("Docs")?.Element("value").Nodes();
                    if (child != null && child.Count() > 0)
                    {
                        XElement returnsEle = new XElement("returns");
                        foreach (var ele in child)
                        {
                            returnsEle.Add(ele);
                        }

                        SpecialProcessElement(returnsEle);
                        docsEle.Add(returnsEle);
                    }
                }
            }

            BatchSpecialProcess(exceptionEles);
            var withChildExceptionList = exceptionEles.Where(p => p.HasElements || !string.IsNullOrEmpty(p.Value)).ToList();
            if (withChildExceptionList != null && withChildExceptionList.Count > 0)
            {
                docsEle.Add(withChildExceptionList);
            }
            m.Docs = docsEle;

            var AssemblyInfoEleList = member.Elements("AssemblyInfo");
            if (AssemblyInfoEleList != null)
            {
                m.AssemblyInfos = new List<string>();
                AssemblyInfoEleList.ToList().ForEach(assemblyInfoEle =>
                {
                    string assemblyName = assemblyInfoEle.Element("AssemblyName")?.Value;
                    var versionEles = assemblyInfoEle.Elements("AssemblyVersion");
                    if (versionEles != null && versionEles.Count() > 0)
                    {
                        versionEles.ToList().ForEach(assVersion =>
                        {
                            m.AssemblyInfos.Add(string.Format("{0}-{1}", assemblyName, assVersion.Value));
                        });
                    }
                });
            }

            if (docsEle.HasElements)
            {
                return m;
            }
            else
            {
                return null;
            }
        }

        private static void BatchSpecialProcess(IEnumerable<XElement> eles)
        {
            if (eles != null)
            {
                eles.ToList().ForEach((Action<XElement>)(p =>
                {
                    Program.SpecialProcessElement((XElement)p);
                }));
            }
        }

        /// <summary>
        /// Some element need special process
        /// </summary>
        /// <param name="ele"></param>
        private static void SpecialProcessElement(XElement ele)
        {
            if (ele != null)
            {
                // Replace href see with content
                // <see href="~/docs/framework/unmanaged-api/diagnostics/isymunmanageddocument-interface.md">ISymUnmanagedDocument</see> => ISymUnmanagedDocument
                var hrefEles = ele.Elements().Where(p => !string.IsNullOrEmpty(p.Attribute("href")?.Value) || _ignoreTags.Contains(p.Name.ToString()));
                if (hrefEles != null && hrefEles.Count() > 0)
                {
                    hrefEles.ToList().ForEach(hrefEle =>
                    {
                        hrefEle.ReplaceWith(hrefEle.Value);
                    });
                }

                var child = ele.Nodes();
                if (child != null && child.Count() > 0)
                {
                    List<XNode> toBeAddEles = new List<XNode>();
                    foreach (var e in child)
                    {
                        if (e.NodeType == System.Xml.XmlNodeType.Text)
                        {
                            if (SpecialProcessText((e as XText)))
                            {
                                toBeAddEles.Add(e);
                            }
                        }
                        else if (e.NodeType == System.Xml.XmlNodeType.Element)
                        {
                            SpecialProcessElement(e as XElement);
                        }
                    }

                    // last text element, need to trim end space
                    var last = child.Last();
                    if (last.NodeType == System.Xml.XmlNodeType.Text)
                    {
                        var xText = last as XText;
                        xText.Value = xText.Value.TrimEnd();
                    }

                    // remove to be add item.
                    if (toBeAddEles.Count > 0)
                    {
                        foreach (var item in toBeAddEles)
                        {
                            item.Remove();
                        }
                    }
                }
            }
        }

        //TODO
        /// Two args('argument', 'eventArgument') are same, need to show different name according to framework version.
        /// ===================================
        /// <summary>
        ///     <Parameters>
        ///         <Parameter Name = "argument" Type="System.String" Index="0" FrameworkAlternate="netframework-1.1" />
        ///         <Parameter Name = "eventArgument" Type="System.String" Index="0" FrameworkAlternate="netframework-2.0;netframework-3.0;netframework-3.5;netframework-4.0;netframework-4.5;netframework-4.5.1;netframework-4.5.2;netframework-4.6;netframework-4.6.1;netframework-4.6.2;netframework-4.7;netframework-4.7.1;netframework-4.7.2;netframework-4.8" />
        ///     </Parameters>
        /// </summary>
        /// ===================================
        /// <param name="member"></param>
        private static void SpecialProcessDuplicateParameters(XElement member)
        {
            var paras = member.Element("Parameters")?.Elements("Parameter").Where(p => p.Attribute("Index") != null).ToList();
            if (paras != null && paras.Count() > 1)
            {
                var indexG = paras.GroupBy(p => p.Attribute("Index").Value);
                indexG.ToList().ForEach(p =>
                {
                    if (p.Count() > 1)
                    {
                        var groupParams = p.ToArray();
                        var docParas = member.Element("Docs").Elements("param");
                        for (int i = 1; i < p.Count(); i++)
                        {
                            var paraName = groupParams[i].Attribute("Name").Value;
                            //string memberName = member.Elements("MemberSignature")?.Where(pp => pp.Attribute("Language").Value == "DocId").First().Attribute("Value").Value;
                            var find = docParas.Where(pa => pa.Attribute("name")?.Value == paraName).FirstOrDefault();
                            if (find != null)
                            {
                                find.Remove();
                            }
                        }
                    }
                });
            }
        }

        // Some xml text need special process
        public static bool SpecialProcessText(XText xText)
        {
            string content = xText.Value;
            bool contentChange = false;
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }
            if (content.ToLower() == "to be added.")
            {
                return true;
            }

            if (content.Length > 5)
            {
                // **Switch Viewing Mode** => Switch Viewing Mode
                foreach (string key in _replaceStringDic.Keys)
                {
                    if (content.Contains(key))
                    {
                        content = content.Replace(key, _replaceStringDic[key]);
                        contentChange = true;
                    }
                }

                // [!INCLUDE[vstecmsbuild](~/includes/vstecmsbuild-md.md)]
                string pattern = "(\\[!INCLUDE.*?\\((.*?)\\)\\])";
                var matches = RegexHelper.GetMatches_All_JustWantedOne(pattern, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        string includeFileFullName = matches[i + 1].Replace("~", _rootFolder);
                        if (File.Exists(includeFileFullName))
                        {
                            string includeFileContent = File.ReadAllText(includeFileFullName);

                            if (!string.IsNullOrEmpty(includeFileContent))
                            {
                                includeFileContent = Regex.Replace(includeFileContent, @"\>\w+", string.Empty, RegexOptions.Multiline);
                                content = content.Replace(matches[i], includeFileContent);
                                contentChange = true;
                            }
                        }
                    }
                }

                // !INCLUDE[linq_dataset]
                pattern = "(!INCLUDE\\[(.*?)\\])";
                matches = RegexHelper.GetMatches_All_JustWantedOne(pattern, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        string includeFileFullName = Path.Combine(_rootFolder, "includes", matches[i + 1]);
                        if (File.Exists(includeFileFullName))
                        {
                            string includeFileContent = File.ReadAllText(includeFileFullName);

                            if (!string.IsNullOrEmpty(includeFileContent))
                            {
                                includeFileContent = Regex.Replace(includeFileContent, @"\>\w+", string.Empty, RegexOptions.Multiline);
                                content = content.Replace(matches[i], includeFileContent);
                                contentChange = true;
                            }
                        }
                    }
                }

                // [ISymUnmanagedWriter Interface](~/docs/framework/unmanaged-api/diagnostics/isymunmanagedwriter-interface.md) => ISymUnmanagedWriter Interface
                pattern = "(\\[(.*?)\\]\\(.*\\))";
                matches = RegexHelper.GetMatches_All_JustWantedOne(pattern, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        content = content.Replace(matches[i], matches[i + 1]);
                        contentChange = true;
                    }
                }

                // **Unix** => Unix
                // __Unix__ => Unix
                pattern = "([_*]{2}([\\w|\\.|\\#|\\+|\\s|/|-]+?)[_*]{2})";
                matches = RegexHelper.GetMatches_All_JustWantedOne(pattern, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        content = content.Replace(matches[i], matches[i + 1]);
                        contentChange = true;
                    }
                }

                // *Unix* => Unix
                // `Unix` => Unix
                // TODO: _Unix_ => Unix, need to identify this case HKEY_CLASSES_ROOT
                pattern = "([\\*|\\`]([\\w|\\.|\\#|\\+|\\s|/|-]+?)[\\*|\\`])";
                matches = RegexHelper.GetMatches_All_JustWantedOne(pattern, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        content = content.Replace(matches[i], matches[i + 1]);
                        contentChange = true;
                    }
                }

                if (contentChange)
                {
                    xText.Value = content;
                }
            }

            return false;
        }

        /// <summary>
        /// Document id have special char, need transfer
        /// </summary>
        /// <param name="docId"></param>
        /// <returns></returns>
        public static string SpecialProcessDocId(string docId)
        {
            if (string.IsNullOrEmpty(docId))
            {
                return docId;
            }
            else
            {
                return docId.Replace("<", "{").Replace(">", "}");
            }
        }

        static FileInfo[] GetFiles(string path, string pattern)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            return di.GetFiles(pattern, SearchOption.AllDirectories).OrderBy(f => f.Name).ToArray();
        }

        static void WriteLine(string format, params object[] args)
        {
            string timestamp = string.Format("[{0}]", DateTime.Now.ToString());
            Console.WriteLine(timestamp + string.Format(format, args));
        }
    }
}
