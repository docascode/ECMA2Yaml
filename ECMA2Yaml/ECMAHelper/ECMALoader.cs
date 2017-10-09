using ECMA2Yaml.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace ECMA2Yaml
{
    public partial class ECMALoader
    {
        private List<string> _errorFiles = new List<string>();
        private ECMADocsTransform _docsTransform = new ECMADocsTransform();
        public Dictionary<string, string> FallbackMapping { get; private set; }

        public ECMAStore LoadFolder(string sourcePath, string fallbackPath)
        {
            if (!Directory.Exists(sourcePath))
            {
                OPSLogger.LogUserWarning(string.Format("Source folder does not exist: {0}", sourcePath));
                return null;
            }

            if (!string.IsNullOrEmpty(fallbackPath) && Directory.Exists(fallbackPath))
            {
                FallbackMapping = GenerateFallbackFileMapping(sourcePath, fallbackPath);
                sourcePath = fallbackPath;
            }

            var frameworks = LoadFrameworks(sourcePath);
            var extensionMethods = LoadExtensionMethods(sourcePath);
            var filterStore = LoadFilters(sourcePath);
            var monikerNugetMapping = LoadMonikerPackageMapping(sourcePath);
            var monikerAssemblyMapping = LoadMonikerAssemblyMapping(sourcePath);

            ConcurrentBag<Namespace> namespaces = new ConcurrentBag<Namespace>();
            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            //foreach(var nsFile in Directory.EnumerateFiles(_baseFolder, "ns-*.xml"))
            Parallel.ForEach(Directory.EnumerateFiles(sourcePath, "ns-*.xml"), opt, nsFile =>
            {
                var nsFileName = Path.GetFileName(nsFile);
                var nsName = nsFileName.Substring("ns-".Length, nsFileName.Length - "ns-.xml".Length);
                if (!string.IsNullOrEmpty(nsName))
                {
                    var ns = LoadNamespace(sourcePath, nsFile);

                    if (ns == null)
                    {
                        OPSLogger.LogUserError("failed to load namespace", nsFile);
                    }
                    else if (ns.Types == null)
                    {
                        OPSLogger.LogUserWarning(string.Format("Namespace {0} has no types", ns.Name), nsFile);
                    }
                    else
                    {
                        namespaces.Add(ns);
                    }
                }
            });

            if (_errorFiles.Count > 0)
            {
                OPSLogger.LogUserError(string.Format("Failed to load {0} files, aborting...", _errorFiles.Count));
                return null;
            }

            var filteredNS = Filter(namespaces, filterStore);
            var store = new ECMAStore(filteredNS.OrderBy(ns => ns.Name).ToArray(), frameworks, extensionMethods, monikerNugetMapping, monikerAssemblyMapping)
            {
                FilterStore = filterStore
            };
            return store;
        }

        private List<Namespace> Filter(IEnumerable<Namespace> namespaces, FilterStore filterStore)
        {
            var filteredNS = namespaces.ToList();
            if (filterStore?.TypeFilters?.Count > 0)
            {
                foreach (var ns in filteredNS)
                {
                    if (ns.Types?.Count > 0)
                    {
                        ns.Types = ns.Types.Where(t =>
                        {
                            bool expose = true;
                            foreach(var filter in filterStore.TypeFilters.Where(tf => tf.Filter(t).HasValue))
                            {
                                expose = expose && filter.Filter(t).Value;
                            }
                            return expose;
                        }).ToList();

                    }
                }
                filteredNS = filteredNS.Where(ns => ns.Types?.Count > 0).ToList();
            }
            if (filterStore?.MemberFilters?.Count > 0)
            {
                foreach (var ns in filteredNS)
                {
                    foreach (var t in ns.Types)
                    {
                        if (t.Members?.Count > 0)
                        {
                            t.Members = t.Members.Where(m =>
                            {
                                bool expose = true;
                                foreach (var filter in filterStore.MemberFilters.Where(mf => mf.Filter(m).HasValue))
                                {
                                    expose = expose && filter.Filter(m).Value;
                                }
                                return expose;
                            }).ToList();
                        }
                    }
                }
            }
            //workaournd for https://github.com/mono/api-doc-tools/issues/89, bug #1022788
            foreach (var ns in filteredNS)
            {
                foreach (var t in ns.Types)
                {
                    if (t.Signatures != null && t.Signatures.ContainsKey("C#") && t.Signatures["C#"].StartsWith("public sealed class"))
                    {
                        t.Members = t.Members.Where(m => !(m.Signatures.ContainsKey("C#") && m.Signatures["C#"].StartsWith("protected "))).ToList();
                    }
                }
            }
            return filteredNS;
        }

        private Namespace LoadNamespace(string basePath, string nsFile)
        {
            XDocument nsDoc = XDocument.Load(Resolve(nsFile));
            Namespace ns = new Namespace();
            ns.Id = ns.Name = nsDoc.Root.Attribute("Name").Value;
            ns.Types = LoadTypes(basePath, ns);
            ns.Docs = LoadDocs(nsDoc.Root.Element("Docs"));
            ns.SourceFileLocalPath = Resolve(nsFile);
            return ns;
        }

        private List<Models.Type> LoadTypes(string basePath, Namespace ns)
        {
            string nsFolder = Path.Combine(basePath, ns.Name);
            if (!Directory.Exists(nsFolder))
            {
                return null;
            }
            List<Models.Type> types = new List<Models.Type>();
            foreach (var typeFile in Directory.EnumerateFiles(nsFolder, "*.xml"))
            {
                var realTypeFile = Resolve(typeFile);
                try
                {
                    var t = LoadType(realTypeFile);
                    t.Parent = ns;
                    types.Add(t);
                }
                catch (Exception ex)
                {
                    OPSLogger.LogUserError(ex.Message, realTypeFile);
                    _errorFiles.Add(realTypeFile);
                }
            }
            return types;
        }

        private Models.Type LoadType(string typeFile)
        {
            string xmlContent = File.ReadAllText(typeFile);
            xmlContent = xmlContent.Replace("TextAntiAliasingQuality&nbsp;property.</summary>", "TextAntiAliasingQuality property.</summary>");
            xmlContent = xmlContent.Replace("DefaultValue('&#x0;')</AttributeName>", "DefaultValue('\\0')</AttributeName>");
            xmlContent = xmlContent.Replace("\0", "\\0");

            XDocument tDoc = XDocument.Parse(xmlContent);
            XElement tRoot = tDoc.Root;
            Models.Type t = new Models.Type();
            t.Name = tRoot.Attribute("Name").Value.Replace('+', '.');
            t.FullName = tRoot.Attribute("FullName").Value.Replace('+', '.');
            t.SourceFileLocalPath = typeFile;

            //TypeSignature
            t.Signatures = new Dictionary<string, string>();
            foreach (var sig in tRoot.Elements("TypeSignature"))
            {
                t.Signatures[sig.Attribute("Language").Value] = sig.Attribute("Value").Value;
            }
            t.DocId = t.Signatures.ContainsKey("DocId") ? t.Signatures["DocId"] : null;
            t.Modifiers = ParseModifiersFromSignatures(t.Signatures);

            //AssemblyInfo
            t.AssemblyInfo = tRoot.Elements("AssemblyInfo")?.Select(a => ParseAssemblyInfo(a)).ToList();

            //TypeParameters
            var tpElement = tRoot.Element("TypeParameters");
            if (tpElement != null)
            {
                t.TypeParameters = tpElement.Elements("TypeParameter")?.Select(tp => new Parameter() { Name = tp.Attribute("Name").Value }).ToList();
            }

            //Parameters
            var pElement = tRoot.Element("Parameters");
            if (pElement != null)
            {
                t.Parameters = pElement.Elements("Parameter").Select(p => Parameter.FromXElement(p)).ToList();
            }

            var rvalElement = tRoot.Element("ReturnValue");
            if (rvalElement != null)
            {
                t.ReturnValueType = new Parameter()
                {
                    Type = rvalElement.Element("ReturnType")?.Value
                };
            }

            //BaseTypeName
            t.BaseType = LoadBaseType(tRoot.Element("Base"));

            //Interfaces
            var interfacesElement = tRoot.Element("Interfaces");
            if (interfacesElement != null)
            {
                t.Interfaces = interfacesElement.Elements("Interface").Select(i => i?.Element("InterfaceName")?.Value).ToList();
            }

            //Attributes
            var attrs = tRoot.Element("Attributes");
            if (attrs != null)
            {
                t.Attributes = attrs.Elements("Attribute").Select(a => LoadAttribute(a)).ToList();
            }

            //Members
            var membersElement = tRoot.Element("Members");
            if (membersElement != null)
            {
                t.Members = membersElement.Elements("Member")?.Select(m => LoadMember(t, m)).ToList();
                t.Members.Sort((m1, m2) =>
                {
                    if (m1.IsEII == m2.IsEII)
                    {
                        return string.Compare(m1.Name, m2.Name);
                    }
                    else
                    {
                        return m1.IsEII ? 1 : -1; //non-EII first, EII later
                    }
                });
                if (t.Members != null)
                {
                    foreach (var m in t.Members)
                    {
                        m.SourceFileLocalPath = typeFile;
                    }
                }
                t.Overloads = membersElement.Elements("MemberGroup")?.Select(m => LoadMemberGroup(t, m)).ToList();
                if (t.Overloads != null)
                {
                    foreach (var m in t.Overloads)
                    {
                        m.SourceFileLocalPath = typeFile;
                    }
                }
            }

            //Docs
            t.Docs = LoadDocs(tRoot.Element("Docs"));

            //MemberType
            t.ItemType = InferTypeOfType(t);

            // Metadata
            t.Metadata = LoadMetadata(tRoot.Element("Metadata"));

            return t;
        }

        private static ItemType InferTypeOfType(Models.Type t)
        {
            var signature = t.Signatures["C#"];
            if (t.BaseType == null && signature.Contains(" interface "))
            {
                return ItemType.Interface;
            }
            else if ("System.Enum" == t.BaseType?.Name && signature.Contains(" enum "))
            {
                return ItemType.Enum;
            }
            else if ("System.Delegate" == t.BaseType?.Name && signature.Contains(" delegate "))
            {
                return ItemType.Delegate;
            }
            else if ("System.ValueType" == t.BaseType?.Name && signature.Contains(" struct "))
            {
                return ItemType.Struct;
            }
            else if (signature.Contains(" class "))
            {
                return ItemType.Class;
            }
            else
            {
                throw new Exception("Unable to identify the type of Type " + t.Uid);
            }
        }

        private BaseType LoadBaseType(XElement bElement)
        {
            if (bElement == null)
            {
                return null;
            }
            BaseType bt = new BaseType();
            bt.Name = bElement.Elements("BaseTypeName")?.LastOrDefault()?.Value;
            var btaElements = bElement.Element("BaseTypeArguments")?.Elements("BaseTypeArgument");
            if (btaElements != null)
            {
                bt.TypeArguments = btaElements.Select(e => new BaseTypeArgument()
                {
                    TypeParamName = e.Attribute("TypeParamName").Value,
                    Value = e.Value
                }).ToList();
            }
            return bt;
        }

        private Member LoadMember(Models.Type t, XElement mElement)
        {
            Member m = new Member();
            m.Parent = t;
            m.Name = mElement.Attribute("MemberName").Value;
            m.ItemType = (ItemType)Enum.Parse(typeof(ItemType), mElement.Element("MemberType").Value);
            if (m.Name.StartsWith("op_") && m.ItemType == ItemType.Method)
            {
                m.ItemType = ItemType.Operator;
            }

            m.Signatures = new Dictionary<string, string>();
            foreach (var sig in mElement.Elements("MemberSignature"))
            {
                m.Signatures[sig.Attribute("Language").Value] = sig.Attribute("Value").Value;
            }
            m.DocId = m.Signatures.ContainsKey("DocId") ? m.Signatures["DocId"] : null;
            m.Modifiers = ParseModifiersFromSignatures(m.Signatures);
            m.AssemblyInfo = mElement.Elements("AssemblyInfo")?.Select(a => ParseAssemblyInfo(a)).ToList();

            //TypeParameters
            var tpElement = mElement.Element("TypeParameters");
            if (tpElement != null)
            {
                m.TypeParameters = tpElement.Elements("TypeParameter").Select(tp => Parameter.FromXElement(tp)).ToList();
            }

            //Parameters
            var pElement = mElement.Element("Parameters");
            if (pElement != null)
            {
                m.Parameters = pElement.Elements("Parameter").Select(p => Parameter.FromXElement(p)).ToList();
            }

            //Attributes
            var attrs = mElement.Element("Attributes");
            if (attrs != null)
            {
                m.Attributes = attrs.Elements("Attribute").Select(a => LoadAttribute(a)).ToList();
            }

            m.ReturnValueType = new Parameter()
            {
                Type = mElement.Element("ReturnValue")?.Element("ReturnType")?.Value
            };

            //Docs
            m.Docs = LoadDocs(mElement.Element("Docs"));

            //hard code this type to minimize workaround impact
            if (t.FullName == "Microsoft.VisualBasic.Collection")
            {
                FixEIIProperty(m);
            }
            
            return m;
        }

        //workaround for https://github.com/mono/api-doc-tools/issues/92, bug #1025217
        private void FixEIIProperty(Member m)
        {
            if (m.ItemType == ItemType.Property && m.Name.Contains('.'))
            {
                var parts = m.Name.Split('.');
                if (parts.Length > 2 && parts.Last().StartsWith(parts[parts.Length - 2]))
                {
                    var name = m.Name.Replace(parts[parts.Length - 2] + "." + parts[parts.Length - 2], parts[parts.Length - 2] + ".");
                    if (m.Signatures?.Count > 0)
                    {
                        var langs = m.Signatures.Keys.ToList();
                        foreach (var lang in langs)
                        {
                            m.Signatures[lang] = m.Signatures[lang].Replace(m.Name, name);
                        }
                    }
                    m.Name = name;
                }
            }
        }

        private ECMAAttribute LoadAttribute(XElement attrElement)
        {
            return new ECMAAttribute()
            {
                Declaration = attrElement.Element("AttributeName").Value,
                Visible = true
            };
        }

        private Member LoadMemberGroup(Models.Type t, XElement mElement)
        {
            Member m = new Member();
            m.Parent = t;
            m.Name = mElement.Attribute("MemberName").Value;
            m.AssemblyInfo = mElement.Elements("AssemblyInfo")?.Select(a => ParseAssemblyInfo(a)).ToList();
            m.Docs = LoadDocs(mElement.Element("Docs"));
            return m;
        }

        public XElement TransformDocs(XElement dElement)
        {
            if (dElement == null)
            {
                return null;
            }

            XElement remarks = dElement.Element("remarks");
            bool skipRemarks = remarks?.Element("format") != null;
            if (remarks != null && skipRemarks)
            {
                remarks.Remove();
            }

            var dElement2 = _docsTransform.Transform(dElement.ToString(), SyntaxLanguage.CSharp).Root;

            if (remarks != null && skipRemarks)
            {
                dElement2.Add(remarks);
            }
            return dElement2;
        }

        public Docs LoadDocs(XElement dElement)
        {
            dElement = TransformDocs(dElement);
            if (dElement == null)
            {
                return null;
            }

            var remarks = dElement.Element("remarks");
            string remarksText = null;
            string examplesText = null;
            if (remarks?.Element("format") != null)
            {
                remarksText = remarks.Element("format").Value;
            }
            else
            {
                remarksText = NormalizeDocsElement(GetInnerXml(remarks));
            }
            if (remarksText != null)
            {
                remarksText = remarksText.Replace("## Remarks", "").Trim();
                if (remarksText.Contains("## Examples"))
                {
                    var pos = remarksText.IndexOf("## Examples");
                    examplesText = remarksText.Substring(pos).Replace("## Examples", "").Trim();
                    remarksText = remarksText.Substring(0, pos).Trim();
                }
            }

            Dictionary<string, string> additionalNotes = null;
            var blocks = dElement.Elements("block")?.Where(p => !string.IsNullOrEmpty(p.Attribute("type").Value)).ToList();
            if (blocks != null && blocks.Count > 0)
            {
                additionalNotes = new Dictionary<string, string>();
                foreach (var block in blocks)
                {
                    additionalNotes[block.Attribute("type").Value] = NormalizeDocsElement(GetInnerXml(block));
                }
            }

            string altCompliant = dElement.Element("altCompliant")?.Attribute("cref")?.Value;
            if (!string.IsNullOrEmpty(altCompliant) && altCompliant.Contains(":"))
            {
                altCompliant = altCompliant.Substring(altCompliant.IndexOf(':') + 1);
            }

            return new Docs()
            {
                Summary = NormalizeDocsElement(GetInnerXml(dElement.Element("summary"))),
                Remarks = remarksText,
                Examples = examplesText,
                AltMemberCommentIds = dElement.Elements("altmember")?.Select(alt => alt.Attribute("cref").Value).ToList(),
                Exceptions = dElement.Elements("exception")?.Select(el => GetTypedContent(el)).ToList(),
                Permissions = dElement.Elements("permission")?.Select(el => GetTypedContent(el)).ToList(),
                Parameters = dElement.Elements("param")?.Where(p => !string.IsNullOrEmpty(p.Attribute("name").Value)).ToDictionary(p => p.Attribute("name").Value, p => NormalizeDocsElement(GetInnerXml(p))),
                TypeParameters = dElement.Elements("typeparam")?.Where(p => !string.IsNullOrEmpty(p.Attribute("name").Value)).ToDictionary(p => p.Attribute("name").Value, p => NormalizeDocsElement(GetInnerXml(p))),
                AdditionalNotes = additionalNotes,
                Returns = NormalizeDocsElement(GetInnerXml(dElement.Element("returns"))), //<value> will be transformed to <returns> by xslt in advance
                ThreadSafety = NormalizeDocsElement(GetInnerXml(dElement.Element("threadsafe"))),
                Since = NormalizeDocsElement(dElement.Element("since")?.Value),
                AltCompliant = altCompliant,
                InternalOnly = dElement.Element("forInternalUseOnly") != null
            };
        }

        private TypedContent GetTypedContent(XElement ele)
        {
            var cref = ele.Attribute("cref").Value;
            return new TypedContent
            {
                CommentId = cref,
                Description = NormalizeDocsElement(GetInnerXml(ele)),
                Uid = cref.Substring(cref.IndexOf(':') + 1).Replace('+', '.')
            };
        }

        private string GetInnerXml(XElement ele)
        {
            if (ele == null)
            {
                return null;
            }
            var reader = ele.CreateReader();
            reader.MoveToContent();
            return reader.ReadInnerXml();
        }

        private static Regex xrefFix = new Regex("<xref:[\\w\\.\\d\\?=]+%[\\w\\.\\d\\?=%]+>", RegexOptions.Compiled);
        private static string NormalizeDocsElement(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Trim() == "To be added.")
            {
                return null;
            }
            return xrefFix.Replace(str.Trim(), m => System.Web.HttpUtility.UrlDecode(m.Value));
        }

        private AssemblyInfo ParseAssemblyInfo(XElement ele)
        {
            var assembly = new AssemblyInfo();
            assembly.Name = ele.Element("AssemblyName")?.Value;
            assembly.Versions = ele.Elements("AssemblyVersion").Select(v => v.Value).ToList();
            return assembly;
        }

        private SortedList<string, List<string>> ParseModifiersFromSignatures(Dictionary<string, string> sigs)
        {
            if (sigs == null)
            {
                return null;
            }

            var modifiers = new SortedList<string, List<string>>();
            if (sigs.ContainsKey("C#"))
            {
                var mods = new List<string>();
                var startWithModifiers = new string[] { "public", "protected", "private"};
                mods.AddRange(startWithModifiers.Where(m => sigs["C#"].StartsWith(m)));
                var containsModifiers = new string[] { "static", "const", "readonly", "sealed", "get;", "set;" };
                mods.AddRange(containsModifiers.Where(m => sigs["C#"].Contains(" " + m + " ")).Select(m => m.Trim(';')));

                if (mods.Any())
                {
                    modifiers.Add("csharp", mods);
                }
            }
            return modifiers;
        }

        private Dictionary<string, object> LoadMetadata(XElement metadataElement)
        {
            if (null != metadataElement)
                return metadataElement.Elements("Meta")?.ToDictionary(x => x.Attribute("Name").Value, x => (object)x.Attribute("Value").Value);
            return new Dictionary<string, object>();
        }
    }
}
