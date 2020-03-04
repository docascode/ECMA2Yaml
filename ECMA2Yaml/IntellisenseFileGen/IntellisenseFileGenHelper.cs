using ECMA2Yaml;
using IntellisenseFileGen.Models;
using Microsoft.OpenPublishing.FileAbstractLayer;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntellisenseFileGen
{
    public class IntellisenseFileGenHelper
    {
        static string _xmlDataFolder = @"G:\SourceCode\DevCode\dotnet-api-docs\xml";
        static string _docsetFolder = @"G:\SourceCode\DevCode\dotnet-api-docs";
        static string _outFolder = @"G:\ECMA2Yaml-output\GenerateIntellisense\_intellisense";
        static string _moniker = string.Empty;
        static string _repoRootFolder = string.Empty;
        static string[] _replaceStringDic = new string[] {
            "1C3C342C96EA43BD96398FAADBD52FF2",@"\\",@"\"
            ,"99AC517E3C8446B48C624569028BE7A2","\\\"","\""
            ,"2BAD1A8DDD5C4C55A920F73420E93A9B",@"\*","*"
            ,"3AB5B1F3925442EE9934073CB9F8F0D6",@"\#","#"
            ,"5EA867E6FA4C4CD0A67956D5FF4BA155",@"\_","_"
            ,"B550F73CF41241B4977E7F607604D4FA",@"\[","["
            ,"20A846118D4744748A0484E79881FFD7",@"\]","]"
        };
        private static string[] _ignoreTags = new string[] { "sup", "b", "csee", "br" };
        static FileAccessor _fileAccessor;

        public static void Start(string[] args)
        {
            var opt = new CommandLineOptions();
            if (opt.Parse(args))
            {
                _xmlDataFolder = opt.XmlPath;
                _docsetFolder = opt.DocsetPath;
                _outFolder = opt.OutFolder;
                _moniker = opt.Moniker;
            }

            if (string.IsNullOrEmpty(_xmlDataFolder))
            {
                // TODO: log error

                return;
            }
            if (!Directory.Exists(_docsetFolder))
            {
                // TODO: log error
                return;
            }
            _repoRootFolder = ECMALoader.GetRepoRootBySubPath(_xmlDataFolder);
            _fileAccessor = new FileAccessor(_repoRootFolder);

            WriteLine(string.Format("xml path:'{0}'", _xmlDataFolder));
            WriteLine(string.Format("docset path:'{0}'", _docsetFolder));
            WriteLine(string.Format("out path:'{0}'", _outFolder));
            WriteLine(string.Format("root path:'{0}'", _repoRootFolder));

            try
            {
                StartGenerate();
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
                throw ex;
                // TODO: log error
            }
        }

        public static void StartGenerate()
        {
            ECMALoader loader = new ECMALoader(_fileAccessor);
            string xmlFolder = _xmlDataFolder.Replace(_repoRootFolder, "").Trim(Path.DirectorySeparatorChar);
            var store = loader.LoadFolder(xmlFolder);
            if (store == null)
            {
                return;
            }
            store.Build();
            var frameworks = store.GetFrameworkIndex();
            List<string> requiredFrameworkList = new List<string>();
            frameworks.FrameworkAssemblies.Keys.ToList().ForEach(fw =>
            {
                if (string.IsNullOrEmpty(_moniker) || _moniker.Equals(fw, StringComparison.OrdinalIgnoreCase))
                {
                    requiredFrameworkList.Add(fw);
                }
            });

            if (requiredFrameworkList.Count == 0)
            {
                string message = string.Empty;
                if (string.IsNullOrEmpty(_moniker))
                {
                    message = string.Format("Generated file failed since found 0 moniker.");
                }
                else
                {
                    message = string.Format("Generated file failed since found 0 moniker with filter moniker name '{0}'.", _moniker);
                }

                throw new Exception(message);
            }

            var typesByDocId = LoadTypes(store).ToDictionary(t => t.DocId, t => t);

            requiredFrameworkList.ForEach(fw =>
            {
                if (string.IsNullOrEmpty(_moniker) || _moniker.Equals(fw, StringComparison.OrdinalIgnoreCase))
                {
                    string outPutFolder = Path.Combine(_outFolder, fw);

                    var fwAssemblyList = frameworks.FrameworkAssemblies[fw].Values.ToList();
                    var fwTypesByAssembly = store.TypesByUid.Values
                    .Where(t => t.Monikers.Contains(fw))
                    .ToLookup(t => t.VersionedAssemblyInfo.ValuesPerMoniker[fw].First());
                    var fwMemberDocIdsByAssembly = store.MembersByUid.Values
                    .Where(m => m.Monikers.Contains(fw))
                    .GroupBy(m => m.VersionedAssemblyInfo.ValuesPerMoniker[fw].First())
                    .ToDictionary(g => g.Key, g => g.Select(m => m.DocId).ToHashSet());

                    fwAssemblyList.ForEach(assembly =>
                    {
                        var assemblyTypes = fwTypesByAssembly[assembly].Select(t => typesByDocId[t.DocId]).ToList();
                        var assemblyMemberDocIds = fwMemberDocIdsByAssembly[assembly];
                        // Order by xml
                        if (assemblyTypes != null && assemblyTypes.Count() > 0)
                        {
                            XDocument intelligenceDoc = new XDocument(new XDeclaration("1.0", "utf-8", null));
                            var docEle = new XElement("doc");
                            var assemblyEle = new XElement("assembly");
                            var membersEle = new XElement("members");
                            docEle.Add(assemblyEle);
                            docEle.Add(membersEle);
                            intelligenceDoc.Add(docEle);
                            assemblyEle.SetElementValue("name", assembly.Name);

                            assemblyTypes.OrderBy(p => p.DocId).ToList().ForEach(tt =>
                            {
                                string id = tt.Uid ?? tt.DocId.Replace("T:", "");
                                if (store.TypesByUid.ContainsKey(id))
                                {
                                    var type = store.TypesByUid[id];
                                    membersEle.Add(SpecialProcessDuplicateParameters(tt.Docs, type, fw));
                                }
                                else
                                {
                                    membersEle.Add(tt.Docs);
                                }

                                if (tt.Members != null && tt.Members.Count() > 0)
                                {
                                    if (tt.Members != null && tt.Members.Count() > 0)
                                    {
                                        tt.Members.OrderBy(p => p.DocId).ToList().ForEach(m =>
                                        {
                                            if (assemblyMemberDocIds.Contains(m.DocId))
                                            {
                                                if (store.MembersByUid.ContainsKey(m.Uid))
                                                {
                                                    var member = store.MembersByUid[m.Uid];
                                                    membersEle.Add(SpecialProcessDuplicateParameters(m.Docs, member, fw));
                                                }
                                                else
                                                {
                                                    membersEle.Add(m.Docs);
                                                }
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
                                WriteLine($"Done generate {fw}.{assembly.Name} intellisense files.");
                            }
                        }
                    });
                }
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
        /// Load all xml file and convert to List<Type>
        /// </summary>
        /// <returns></returns>
        public static List<Models.Type> LoadTypes(ECMA2Yaml.Models.ECMAStore store)
        {
            string xmlFolder = _xmlDataFolder.Replace(_repoRootFolder, "").Trim(Path.DirectorySeparatorChar);
            var typeFileList = GetFiles(xmlFolder, "**\\*.xml");
            ConcurrentBag<Models.Type> typeList = new ConcurrentBag<Models.Type>();
            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.ForEach(typeFileList, opt, typeFile =>
            {
                XDocument xmlDoc = XDocument.Load(typeFile.AbsolutePath);

                if (xmlDoc.Root.Name.LocalName == "Type")
                {
                    Models.Type t = ConvertToType(xmlDoc, store);
                    if (t != null)
                    {
                        typeList.Add(t);
                    }
                }
            });

            return typeList.ToList();
        }

        /// <summary>
        /// Convert xml node(<Type></Type>) to Type object
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        private static Models.Type ConvertToType(XDocument xmlDoc, ECMA2Yaml.Models.ECMAStore store)
        {
            Models.Type t = new Models.Type();

            string docId = GetDocId(xmlDoc.Root, "TypeSignature");
            if (!string.IsNullOrEmpty(docId))
            {
                t.DocId = docId;
            }
            if (!store.ItemsByDocId.ContainsKey(docId))
            {
                return null;
            }

            var docsEle = new XElement("member");
            SetDocsEle(docsEle, xmlDoc.Root, docId);
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
                    var m = ConvertToMember(memberEle, store);
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
        private static Member ConvertToMember(XElement member, ECMA2Yaml.Models.ECMAStore store)
        {
            var m = new Member();

            string docId = GetDocId(member, "MemberSignature");
            if (!string.IsNullOrEmpty(docId))
            {
                m.DocId = docId;
            }
            else
            {
                throw new Exception("DocId missing for member " + member.Attribute("MemberName")?.Value);
            }

            if (store.ItemsByDocId.TryGetValue(docId, out var item))
            {
                m.CommentId = item.CommentId;
                m.Uid = item.Uid;
            }
            else
            {
                return null;
            }

            var docsEle = new XElement("member");
            SetDocsEle(docsEle, member, m.CommentId);
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

        private static void SetDocsEle(XElement docsEle, XElement xmlEle, string uid)
        {
            if (xmlEle == null || docsEle == null)
            {
                return;
            }

            var summaryEle = xmlEle.Element("Docs")?.Element("summary");
            var paramEles = xmlEle.Element("Docs")?.Elements("param");
            var typeparamEles = xmlEle.Element("Docs")?.Elements("typeparam");
            var exceptionEles = xmlEle.Element("Docs")?.Elements("exception");

            if (summaryEle != null)
            {
                SpecialProcessElement(summaryEle);

                if (!string.IsNullOrEmpty(summaryEle?.Value) || summaryEle.HasElements)
                {
                    docsEle.Add(summaryEle);
                }
            }

            if (paramEles != null && paramEles.Count() > 0)
            {
                BatchSpecialProcess(paramEles);
                docsEle.Add(paramEles);
            }
            else
            {
                var paras = xmlEle.Element("Parameters")?.Elements("Parameter");
                if (paras != null && paras.Count() > 0)
                {
                    paras.ToList().ForEach(p =>
                    {
                        var mPara = new XElement("param");
                        mPara.SetAttributeValue("name", p.Attribute("Name")?.Value);
                        docsEle.Add(mPara);
                    });
                }
            }

            if (typeparamEles != null && typeparamEles.Count() > 0)
            {
                BatchSpecialProcess(typeparamEles);
                docsEle.Add(typeparamEles);
            }
            else
            {
                var paras = xmlEle.Element("TypeParameters")?.Elements("TypeParameter");
                if (paras != null && paras.Count() > 0)
                {
                    paras.ToList().ForEach(p =>
                    {
                        var tPara = new XElement("typeparam");
                        tPara.SetAttributeValue("name", p.Attribute("Name")?.Value);
                        docsEle.Add(tPara);
                    });
                }
            }

            if (exceptionEles != null && exceptionEles.Count() > 0)
            {
                BatchSpecialProcess(exceptionEles);
                docsEle.Add(exceptionEles);
            }

            // Returns
            if (xmlEle.Element("Docs")?.Element("returns") != null)
            {
                if (xmlEle.Element("Docs")?.Element("returns").Value != "To be added.")
                {
                    var returnEle = xmlEle.Element("Docs")?.Element("returns");
                    SpecialProcessElement(returnEle);
                    docsEle.Add(returnEle);
                }
            }
            else if (xmlEle.Element("Docs")?.Element("value") != null)
            {
                if (xmlEle.Element("Docs")?.Element("value").Value != "To be added.")
                {
                    var child = xmlEle.Element("Docs")?.Element("value").Nodes();
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

            docsEle.SetAttributeValue("name", uid);
        }

        private static string GetDocId(XElement xmlEle, string signatureName = "MemberSignature")
        {
            string docId = string.Empty;
            if (xmlEle == null)
            {
                return docId;
            }

            var docIdEle = xmlEle.Elements(signatureName)?.Where(p => p.Attribute("Language").Value == "DocId").LastOrDefault();
            if (docIdEle != null)
            {
                docId = docIdEle.Attribute("Value").Value;
            }

            return docId;
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
                if (ele.Name == "c") return;
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

                // <summary><format type="text/markdown"><![CDATA[Describes the common properties that all features have.]]></format></summary> 
                // => 
                // <summary>Describes the common properties that all features have.</summary>
                var formatEles = ele.Elements().Where(p=>p.Name=="format");
                if (formatEles != null && formatEles.Count() > 0)
                {
                    formatEles.ToList().ForEach(formatEle =>
                    {
                        formatEle.ReplaceWith(formatEle.Value);
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
        public static XElement SpecialProcessDuplicateParameters(XElement obj, ECMA2Yaml.Models.ReflectionItem item, string targetFw)
        {
            if (item == null || item.Parameters == null || item.Parameters.Count == 0) return obj;

            XElement changeObj = new XElement(obj);
            var docParas = changeObj.Elements("param");
            if (docParas == null || docParas.Count() == 0) return obj;

            item.Parameters.Where(p => p.Index.HasValue).ToList().ForEach(p =>
            {
                if (p.VersionedNames != null)
                {
                    foreach (var vn in p.VersionedNames)
                    {
                        if (vn.Monikers != null)
                        {
                            if (!vn.Monikers.Contains(targetFw))
                            {
                                // remove
                                var find = docParas.Where(pa => pa.Attribute("name")?.Value == vn.Value).FirstOrDefault();
                                if (find != null)
                                {
                                    find.Remove();
                                }
                            }
                        }
                    }
                }
            });

            return changeObj;
        }

        // Some xml text need special process
        public static bool SpecialProcessText(XText xText)
        {
            string content = xText.Value;
            bool contentChange = false;
            Dictionary<string, string> localReplaceStringDic = new Dictionary<string, string>();
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
                //```csharp this is a test page```
                var matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.TripleSytax_Pattern1, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        string guid = Guid.NewGuid().ToString("N");
                        content = content.Replace(matches[i], guid);
                        localReplaceStringDic.Add(guid, matches[i + 1]);

                        contentChange = true;
                    }
                }

                //```this is a test page```
                matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.TripleSytax_Pattern2, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        string guid = Guid.NewGuid().ToString("N");
                        content = content.Replace(matches[i], guid);
                        localReplaceStringDic.Add(guid, matches[i + 1]);

                        contentChange = true;
                    }
                }

                // `Unix` => guid(Unix)
                // Content between two `, is origin content, don't need escape
                // following demo, *..* is origin content, don't need to escape
                // ============================================================================
                // JSON comment within `/*..*/`.      ==>       JSON comment within /*..*/
                // ============================================================================
                // We need to protect /*..*/, put it into a dic(localReplaceStringDic), replace it with a guid, 
                // After other things done, we need replace the content back
                matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.SingleSytax_Pattern2, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        string guid = Guid.NewGuid().ToString("N");
                        content = content.Replace(matches[i], guid);
                        localReplaceStringDic.Add(guid, matches[i + 1]);

                        contentChange = true;
                    }
                }

                // \* => 2BAD1A8DDD5C4C55A920F73420E93A9B
                for (int i = 0; i < _replaceStringDic.Length - 1; i += 3)
                {
                    if (content.Contains(_replaceStringDic[i + 1]))
                    {
                        content = content.Replace(_replaceStringDic[i + 1], _replaceStringDic[i]);
                        contentChange = true;
                    }
                }

                // [!INCLUDE[vstecmsbuild](~/includes/vstecmsbuild-md.md)]
                matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.Include_Pattern1, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        string includeFileFullName = matches[i + 1].Replace("~", _docsetFolder);
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
                matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.Include_Pattern2, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        string includeFileFullName = Path.Combine(_docsetFolder, "includes", matches[i + 1]);
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
                matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.Link_Pattern, content);
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
                matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.DoubleSytax_Pattern, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        content = content.Replace(matches[i], matches[i + 1]);
                        contentChange = true;
                    }
                }

                // *Unix* => Unix
                // TODO: _Unix_ => Unix, need to identify this case HKEY_CLASSES_ROOT
                matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.SingleSytax_Pattern1, content);
                if (matches != null && matches.Length >= 2)
                {
                    for (int i = 0; i < matches.Length; i += 2)
                    {
                        content = content.Replace(matches[i], matches[i + 1]);
                        contentChange = true;
                    }
                }

                // 2BAD1A8DDD5C4C55A920F73420E93A9B => *
                for (int i = 0; i < _replaceStringDic.Length - 1; i += 3)
                {
                    if (content.Contains(_replaceStringDic[i]))
                    {
                        content = content.Replace(_replaceStringDic[i], _replaceStringDic[i + 2]);
                        contentChange = true;
                    }
                }

                // guid(Unix) => Unix
                if (localReplaceStringDic.Keys != null && localReplaceStringDic.Keys.Count() > 0)
                {
                    localReplaceStringDic.ToList().ForEach(p =>
                    {
                        content = content.Replace(p.Key, p.Value);
                    });
                }

                if (contentChange)
                {
                    xText.Value = content;
                }
            }

            return false;
        }

        static IEnumerable<FileItem> GetFiles(string subFolder, string glob)
        {
            return _fileAccessor.ListFiles(new string[] { glob }, subFolder: subFolder);
        }

        static void WriteLine(string format, params object[] args)
        {
            string timestamp = string.Format("[{0}]", DateTime.Now.ToString());
            Console.WriteLine(timestamp + string.Format(format, args));
        }
    }
}
