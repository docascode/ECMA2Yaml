using ECMA2Yaml.Models;
using Microsoft.OpenPublishing.FileAbstractLayer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Path = System.IO.Path;

namespace ECMA2Yaml
{
    public partial class ECMALoader
    {
        private List<string> _errorFiles = new List<string>();
        private ECMADocsTransform _docsTransform = new ECMADocsTransform();
        private FileAccessor _fileAccessor = null;

        public ConcurrentBag<string> FallbackFiles { get; private set; }

        public ECMALoader(FileAccessor fileAccessor)
        {
            _fileAccessor = fileAccessor;
            FallbackFiles = new ConcurrentBag<string>();
        }

        public ECMAStore LoadFolder(string sourcePath)
        {
            //if (!System.IO.Directory.Exists(sourcePath))
            //{
            //    OPSLogger.LogUserWarning(string.Format("Source folder does not exist: {0}", sourcePath));
            //    return null;
            //}

            var frameworks = LoadFrameworks(sourcePath);
            //var extensionMethods = LoadExtensionMethods(sourcePath);
            var filterStore = LoadFilters(sourcePath);
            var monikerNugetMapping = LoadMonikerPackageMapping(sourcePath);
            var monikerAssemblyMapping = LoadMonikerAssemblyMapping(sourcePath);

            ConcurrentBag<Namespace> namespaces = new ConcurrentBag<Namespace>();
            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            //foreach(var nsFile in ListFiles(sourcePath, Path.Combine(sourcePath, "ns-*.xml")))
            Parallel.ForEach(ListFiles(sourcePath, Path.Combine(sourcePath, "ns-*.xml")), opt, nsFile =>
            {
                var ns = LoadNamespace(sourcePath, nsFile);
                if (ns == null)
                {
                    OPSLogger.LogUserError("failed to load namespace", nsFile.AbsolutePath);
                }
                else if (ns.Types == null)
                {
                    OPSLogger.LogUserWarning(string.Format("Namespace {0} has no types", ns.Name), nsFile.AbsolutePath);
                }
                else
                {
                    namespaces.Add(ns);
                    if (nsFile.IsVirtual)
                    {
                        FallbackFiles.Add(nsFile.AbsolutePath);
                    }
                }
            });

            if (_errorFiles.Count > 0)
            {
                OPSLogger.LogUserError(string.Format("Failed to load {0} files, aborting...", _errorFiles.Count));
                return null;
            }

            var filteredNS = Filter(namespaces, filterStore);
            var store = new ECMAStore(filteredNS.OrderBy(ns => ns.Name).ToArray(), frameworks, /*extensionMethods,*/ monikerNugetMapping, monikerAssemblyMapping)
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
                            var applicableFilters = filterStore.TypeFilters.Where(tf => tf.Filter(t).HasValue).ToList();
                            if (applicableFilters.Count == 1)
                            {
                                expose = applicableFilters.First().Filter(t).Value;
                            }
                            else if (applicableFilters.Count > 1)
                            {
                                var filtersPerNS = applicableFilters.GroupBy(tf => tf.Namespace).Select(tfg => tfg.FirstOrDefault(tf => tf.Name != "*") ?? tfg.First()).ToList();
                                foreach (var filter in filtersPerNS)
                                {
                                    expose = expose && filter.Filter(t).Value;
                                }
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

        private Namespace LoadNamespace(string basePath, FileItem nsFile)
        {
            XDocument nsDoc = XDocument.Load(nsFile.AbsolutePath);
            Namespace ns = new Namespace();
            ns.Id = ns.Name = nsDoc.Root.Attribute("Name").Value;
            ns.Types = LoadTypes(basePath, ns);
            ns.Docs = LoadDocs(nsDoc.Root.Element("Docs"));
            ns.SourceFileLocalPath = nsFile.AbsolutePath;
            ns.ItemType = ItemType.Namespace;
            return ns;
        }

        private List<Models.Type> LoadTypes(string basePath, Namespace ns)
        {
            string nsFolder = Path.Combine(basePath, ns.Name);
            List<Models.Type> types = new List<Models.Type>();
            foreach (var typeFile in ListFiles(nsFolder, Path.Combine(nsFolder, "*.xml")))
            {
                try
                {
                    var t = LoadType(typeFile);
                    if (t != null)
                    {
                        t.Parent = ns;
                        types.Add(t);
                        if (typeFile.IsVirtual)
                        {
                            FallbackFiles.Add(typeFile.AbsolutePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OPSLogger.LogUserError(ex.Message, typeFile.AbsolutePath);
                    _errorFiles.Add(typeFile.AbsolutePath);
                }
            }
            return types;
        }

        private Models.Type LoadType(FileItem typeFile)
        {
            string xmlContent = _fileAccessor.ReadAllText(typeFile.RelativePath);
            xmlContent = xmlContent.Replace("TextAntiAliasingQuality&nbsp;property.</summary>", "TextAntiAliasingQuality property.</summary>");
            xmlContent = xmlContent.Replace("DefaultValue('&#x0;')</AttributeName>", "DefaultValue('\\0')</AttributeName>");
            xmlContent = xmlContent.Replace("\0", "\\0");

            XDocument tDoc = XDocument.Parse(xmlContent, LoadOptions.PreserveWhitespace);
            XElement tRoot = tDoc.Root;
            if (tRoot.Name.LocalName != "Type")
            {
                return null;
            }
            Models.Type t = new Models.Type();
            t.Name = tRoot.Attribute("Name").Value.Replace('+', '.');
            t.FullName = tRoot.Attribute("FullName").Value.Replace('+', '.');
            t.SourceFileLocalPath = typeFile.AbsolutePath;

            //TypeSignature
            t.Signatures = new Dictionary<string, string>();
            foreach (var sig in tRoot.Elements("TypeSignature"))
            {
                t.Signatures[sig.Attribute("Language").Value] = sig.Attribute("Value").Value;
            }
            t.DocId = t.Signatures.ContainsKey("DocId") ? t.Signatures["DocId"] : null;
            t.Modifiers = ParseModifiersFromSignatures(t.Signatures);

            //AssemblyInfo
            t.AssemblyInfo = tRoot.Elements("AssemblyInfo")?.SelectMany(a => ParseAssemblyInfo(a)).ToList();

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
                        m.SourceFileLocalPath = typeFile.AbsolutePath;
                    }
                }
                t.Overloads = membersElement.Elements("MemberGroup")?.Select(m => LoadMemberGroup(t, m)).ToList();
                if (t.Overloads != null)
                {
                    var distinctList = new List<Member>();
                    foreach(var og in t.Overloads.GroupBy(o => o.Name))
                    {
                        if (og.Count() > 1)
                        {
                            OPSLogger.LogUserWarning("Found duplicated <MemberGroup> " + og.Key, typeFile.AbsolutePath);
                        }
                        og.First().SourceFileLocalPath = typeFile.AbsolutePath;
                        distinctList.Add(og.First());
                    }
                    t.Overloads = distinctList;
                }
            }

            //Docs
            t.Docs = LoadDocs(tRoot.Element("Docs"));

            //MemberType
            t.ItemType = InferTypeOfType(t);

            // Metadata
            t.ExtendedMetadata = LoadMetadata(tRoot.Element("Metadata"));

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
                throw new Exception("Unable to identify the type of Type " + t.Name);
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
            var memberValue = mElement.Element("MemberValue")?.Value;
            if (!string.IsNullOrEmpty(memberValue))
            {
                m.Metadata[OPSMetadata.LiteralValue] = memberValue;
            }

            m.Signatures = new Dictionary<string, string>();
            foreach (var sig in mElement.Elements("MemberSignature"))
            {
                var val = sig.Attribute("Value");
                if (val != null)
                {
                    m.Signatures[sig.Attribute("Language").Value] = val.Value;
                }
            }
            m.DocId = m.Signatures.ContainsKey("DocId") ? m.Signatures["DocId"] : null;
            m.Modifiers = ParseModifiersFromSignatures(m.Signatures, m);
            m.AssemblyInfo = mElement.Elements("AssemblyInfo")?.SelectMany(a => ParseAssemblyInfo(a)).ToList();

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
                if (m.Parameters.All(p => !string.IsNullOrEmpty(p.Index)))
                {
                    //foreach(var group in m.Parameters.GroupBy(p => p.Index))
                    //{
                    //    if (group.Select(p => p.Type).Distinct().Count() > 1)
                    //    {
                    //        Console.WriteLine(t.FullName);
                    //        Console.WriteLine(m.DocId);
                    //    }
                    //}
                    m.Parameters = m.Parameters.GroupBy(p => p.Index).OrderBy(g => g.Key).Select(g => g.First()).ToList();
                }
            }

            //Attributes
            var attrs = mElement.Element("Attributes");
            if (attrs != null)
            {
                m.Attributes = attrs.Elements("Attribute").Select(a => LoadAttribute(a)).ToList();
            }

            var returnTypeStr = mElement.Element("ReturnValue")?.Element("ReturnType")?.Value;
            if (returnTypeStr != null)
            {
                var returnType = new Parameter()
                {
                    Type = returnTypeStr,
                    OriginalTypeString = returnTypeStr
                };
                if (returnType.Type.EndsWith("&"))
                {
                    returnType.Type = returnType.Type.TrimEnd('&');
                    returnType.RefType = "ref";
                }
                returnType.Type = returnType.Type.Replace('+', '.');
                m.ReturnValueType = returnType;
            }

            var implements = mElement.Element("Implements");
            if (implements != null)
            {
                m.Implements = implements.Elements("InterfaceMember")?.Select(ele => ele.Value).ToList();
            }

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

        private Member LoadMemberGroup(Models.Type t, XElement mElement)
        {
            Member m = new Member();
            m.Parent = t;
            m.Name = mElement.Attribute("MemberName").Value;
            m.AssemblyInfo = mElement.Elements("AssemblyInfo")?.SelectMany(a => ParseAssemblyInfo(a)).ToList();
            m.Docs = LoadDocs(mElement.Element("Docs"));
            return m;
        }
    }
}
