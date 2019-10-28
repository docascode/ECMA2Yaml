using IntellisenseFileGen.Models;
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
    class Program1
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

        static void Main1(string[] args)
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
            var frameworkInfo = LoadFrameworks();
            var typeList = LoadTypes();
            var monikerAssemblyList = LoadMonikerAssemblyList();
            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };

            frameworkInfo.FrameworkAssemblies.Keys.ToList().ForEach(fw =>
            {
                //if (fw == "netframework-4.6")
                //{
                string outPutFolder = Path.Combine(_outFolder, fw);

                var currentMonikerAssemblyNameList = monikerAssemblyList[fw];
                var fwAssemblyList = frameworkInfo.FrameworkAssemblies[fw].Where(p => { return currentMonikerAssemblyNameList.Contains(p.Name); }).ToList();
                var fwDocIdList = frameworkInfo.NamespaceDocIdsDict[fw];
                var fwTypeDocIdList = frameworkInfo.NamespaceDocIdsDict[fw].Where(p => { return string.IsNullOrEmpty(p.ParentDocId); }).Select(p => p.DocId);
                var fwMemberDocIdList = frameworkInfo.NamespaceDocIdsDict[fw].Where(p => { return !string.IsNullOrEmpty(p.ParentDocId); }).Select(p => p.DocId);

                Parallel.ForEach(fwAssemblyList, opt, assembly =>
                {
                    string assemblyInfoStr = string.Format("{0}-{1}", assembly.Name, assembly.Version);
                    var assemblyTypes = typeList.Where(t =>
                    {
                        return t.AssemblyInfos.Exists(p => { return p == assemblyInfoStr; });
                    }).ToList();

                    // 1. Order by xml
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
                        assemblyEle.SetElementValue("name", assembly.Name);

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
                            intelligenceDoc.Save(Path.Combine(outPutFolder, assembly.Name + ".xml"));
                            WriteLine($"Done generate {assembly.Name} intellisense files.");
                        }
                    }
                });
                //}
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
                        var t = SpecialProcessDocId(tElement.Attribute("Id").Value);
                        frameworkIndex.NamespaceDocIdsDict[fxName].Add(new FrameworkDocIdInfo() { DocId = t, Namespace = ns });
                        foreach (var mElement in tElement.Elements("Member"))
                        {
                            var m = SpecialProcessDocId(mElement.Attribute("Id").Value);
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
        public static List<Models.Type> LoadTypes()
        {
            var typeFileList = GetFiles(_xmlDataFolder, "*.xml");
            List<Models.Type> typeList = new List<Models.Type>();
            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.ForEach(typeFileList, opt, typeFile =>
            {
                XDocument xmlDoc = XDocument.Load(typeFile.FullName);

                if (xmlDoc.Root.Name.LocalName == "Type")
                {
                    Models.Type t = ConvertToType(xmlDoc);
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
        private static Models.Type ConvertToType(XDocument xmlDoc)
        {
            Models.Type t = new Models.Type();

            var typeDocIdEle = xmlDoc.Root.Elements("TypeSignature")?.Where(p => p.Attribute("Language").Value == "DocId")?.FirstOrDefault();
            if (typeDocIdEle != null)
            {
                t.DocId = SpecialProcessDocId(typeDocIdEle.Attribute("Value").Value);
            }

            // for debug
            //if (!IsMeetDebugCondition(t.DocId))
            //{
            //    return null;
            //}

            var docsEle = new XElement("member");
            var typeSummaryEle = xmlDoc.Root.Element("Docs")?.Element("summary");
            var paramEles = xmlDoc.Root.Element("Docs")?.Elements("param");
            var typeparamEles = xmlDoc.Root.Element("Docs")?.Elements("typeparam");
            SpecialProcessElement(typeSummaryEle);
            if (string.IsNullOrEmpty(typeSummaryEle?.Value))
            {
                return null;
            }
            BatchSpecialProcess(paramEles);
            BatchSpecialProcess(typeparamEles);

            docsEle.SetAttributeValue("name", t.DocId);
            docsEle.Add(typeSummaryEle);
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
                    var m = ConvertToMember(memberEle);
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
        private static Member ConvertToMember(XElement member)
        {
            var m = new Member();
            var memberDocIdEle = member.Elements("MemberSignature")?.Where(p => p.Attribute("Language").Value == "DocId").First();
            if (memberDocIdEle != null)
            {
                m.DocId = SpecialProcessDocId(memberDocIdEle.Attribute("Value").Value);
            }

            // for debug
            //if (!IsMeetDebugCondition(m.DocId))
            //{
            //    return null;
            //}

            var memberSummaryEle = member.Element("Docs")?.Element("summary");
            var paramEles = member.Element("Docs")?.Elements("param");
            var typeparamEles = member.Element("Docs")?.Elements("typeparam");
            var exceptionEles = member.Element("Docs")?.Elements("exception");

            var docsEle = new XElement("member");
            docsEle.SetAttributeValue("name", m.DocId);
            SpecialProcessElement(memberSummaryEle);
            if (string.IsNullOrEmpty(memberSummaryEle?.Value))
            {
                return null;
            }
            docsEle.Add(memberSummaryEle);
            BatchSpecialProcess(paramEles);
            docsEle.Add(paramEles);
            BatchSpecialProcess(typeparamEles);
            docsEle.Add(typeparamEles);

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
            docsEle.Add(exceptionEles);
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

            return m;
        }

        private static void BatchSpecialProcess(IEnumerable<XElement> eles)
        {
            if (eles != null)
            {
                eles.ToList().ForEach((Action<XElement>)(p =>
                {
                    SpecialProcessElement((XElement)p);
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

                //string pattern = "(\\n\\s+\\n)";
                //var matches = RegexHelper.GetMatches_All_JustWantedOne(pattern, content);
                //if (matches != null && matches.Length >= 1)
                //{
                //    content = content.Replace(matches[0], "\n");
                //    contentChange = true;
                //}

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

                // [ISymUnmanagedWriter Interface](~/docs/framework/unmanaged-api/diagnostics/isymunmanagedwriter-interface.md) => ISymUnmanagedWriter Interface
                pattern = "(\\[(.*?)\\]\\(.*?\\))";
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

        static bool IsMeetDebugCondition(string condition)
        {
            string[] conditions = new[] { "T:System.Globalization.NumberStyles", "F:System.Globalization.NumberStyles.AllowExponent" };
            if (conditions != null && conditions.Contains(condition))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
