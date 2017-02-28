using ECMA2Yaml.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ECMA2Yaml
{
    public class ECMALoader
    {
        private string _baseFolder;
        private List<string> _errorFiles = new List<string>();
        private ECMADocsTransform _docsTransform = new ECMADocsTransform();

        public ECMAStore LoadFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                OPSLogger.LogUserError(string.Format("Source folder does not exist: {0}", path));
                return null;
            }

            var frameworks = LoadFrameworks(path);

            _baseFolder = path;
            ConcurrentBag<Namespace> namespaces = new ConcurrentBag<Namespace>();
            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            //foreach(var nsFile in Directory.EnumerateFiles(_baseFolder, "ns-*.xml"))
            Parallel.ForEach(Directory.EnumerateFiles(_baseFolder, "ns-*.xml"), opt, nsFile =>
            {
                var nsFileName = Path.GetFileName(nsFile);
                var nsName = nsFileName.Substring("ns-".Length, nsFileName.Length - "ns-.xml".Length);
                if (!string.IsNullOrEmpty(nsName))
                {
                    namespaces.Add(LoadNamespace(nsFile));
                }
            });

            if (_errorFiles.Count > 0)
            {
                OPSLogger.LogUserError(string.Format("Failed to load {0} files, aborting...", _errorFiles.Count));
                return null;
            }

            return new ECMAStore(namespaces.OrderBy(ns => ns.Name).ToArray(), frameworks);
        }

        private Dictionary<string, List<string>> LoadFrameworks(string path)
        {
            var frameworkFolder = Path.Combine(path, "FrameworksIndex");
            if (!Directory.Exists(frameworkFolder))
            {
                return null;
            }
            Dictionary<string, List<string>> frameworks = new Dictionary<string, List<string>>();
            foreach (var fxFile in Directory.EnumerateFiles(frameworkFolder, "*.xml"))
            {
                XDocument fxDoc = XDocument.Load(fxFile);
                var fxName = fxDoc.Root.Attribute("Name").Value.Replace(".", "");
                foreach (var tElement in fxDoc.Root.Elements("Type"))
                {
                    var t = tElement.Attribute("Name").Value.Replace('/', '.');
                    frameworks.AddWithKeys(t, null, fxName);
                    foreach (var mElement in tElement.Elements("Member"))
                    {
                        var mSig = mElement.Attribute("Sig").Value;
                        frameworks.AddWithKeys(t, mSig, fxName);
                    }
                }
            }

            return frameworks;
        }

        private Namespace LoadNamespace(string nsFile)
        {
            XDocument nsDoc = XDocument.Load(nsFile);
            Namespace ns = new Namespace();
            ns.Id = ns.Name = nsDoc.Root.Attribute("Name").Value;
            ns.Types = LoadTypes(ns);
            ns.Docs = LoadDocs(nsDoc.Root.Element("Docs"));

            return ns;
        }

        private List<Models.Type> LoadTypes(Namespace ns)
        {
            string nsFolder = Path.Combine(_baseFolder, ns.Name);
            if (!Directory.Exists(nsFolder))
            {
                return null;
            }
            List<Models.Type> types = new List<Models.Type>();
            foreach (var typeFile in Directory.EnumerateFiles(nsFolder, "*.xml"))
            {
                try
                {
                    var t = LoadType(typeFile);
                    t.Parent = ns;
                    types.Add(t);
                }
                catch (Exception ex)
                {
                    OPSLogger.LogUserError(ex.Message, typeFile);
                    _errorFiles.Add(typeFile);
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
            t.Name = tRoot.Attribute("Name").Value;
            t.FullName = tRoot.Attribute("FullName").Value;

            //TypeSignature
            t.Signatures = new Dictionary<string, string>();
            foreach (var sig in tRoot.Elements("TypeSignature"))
            {
                t.Signatures[sig.Attribute("Language").Value] = sig.Attribute("Value").Value;
            }

            //AssemblyInfo
            t.AssemblyInfo = tRoot.Elements("AssemblyInfo")?.Select(a => ParseAssemblyInfo(a)).ToList();

            //TypeParameters
            var tpElement = tRoot.Element("TypeParameters");
            if (tpElement != null)
            {
                t.TypeParameters = tpElement.Elements("TypeParameter")?.Select(tp => new Parameter() { Name = tp.Attribute("Name").Value }).ToList();
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
                t.Attributes = attrs.Elements("Attribute").Select(a => a.Element("AttributeName").Value).ToList();
            }

            //Members
            var membersElement = tRoot.Element("Members");
            if (membersElement != null)
            {
                t.Members = membersElement.Elements("Member").Select(m => LoadMember(t, m)).ToList();
            }

            //Docs
            t.Docs = LoadDocs(tRoot.Element("Docs"));

            //MemberType
            t.MemberType = InferTypeOfType(t);
            return t;
        }

        private static MemberType InferTypeOfType(Models.Type t)
        {
            var signature = t.Signatures["C#"];
            if (t.BaseType == null && signature.Contains(" interface "))
            {
                return MemberType.Interface;
            }
            else if ("System.Enum" == t.BaseType?.Name && signature.Contains(" enum "))
            {
                return MemberType.Enum;
            }
            else if ("System.Delegate" == t.BaseType?.Name && signature.Contains(" delegate "))
            {
                return MemberType.Delegate;
            }
            else if ("System.ValueType" == t.BaseType?.Name && signature.Contains(" struct "))
            {
                return MemberType.Struct;
            }
            else if (signature.Contains(" class "))
            {
                return MemberType.Class;
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
            bt.Name = bElement.Element("BaseTypeName")?.Value;
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
            m.MemberType = (MemberType)Enum.Parse(typeof(MemberType), mElement.Element("MemberType").Value);
            if (m.Name.StartsWith("op_") && m.MemberType == MemberType.Method)
            {
                m.MemberType = MemberType.Operator;
            }

            m.Signatures = new Dictionary<string, string>();
            foreach (var sig in mElement.Elements("MemberSignature"))
            {
                m.Signatures[sig.Attribute("Language").Value] = sig.Attribute("Value").Value;
            }

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
                m.Attributes = attrs.Elements("Attribute").Select(a => a.Element("AttributeName").Value).ToList();
            }

            m.ReturnValueType = new Parameter()
            {
                Type = mElement.Element("ReturnValue")?.Element("ReturnType")?.Value
            };

            //Docs
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
            remarks?.Remove();

            var dElement2 = _docsTransform.Transform(dElement.ToString(), SyntaxLanguage.CSharp).Root;

            if (remarks != null)
            {
                dElement2.Add(remarks);
            }
            return dElement2;
        }

        private static Regex xrefFix = new Regex("&lt;(xref:[\\w\\.]*)(%2A)?&gt;", RegexOptions.Compiled);

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
                remarksText = NormalizeDocsElement(remarks?.Value);
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

            return new Docs()
            {
                Summary = NormalizeDocsElement(GetInnerXml(dElement.Element("summary"))),
                Remarks = remarksText,
                Examples = examplesText,
                AltMembers = dElement.Elements("altmember")?.ToList(),
                Exceptions = dElement.Elements("exception")?.Select(el =>
                {
                    var cref = el.Attribute("cref").Value;
                    return new ExceptionDef
                    {
                        CommentId = cref,
                        Description = NormalizeDocsElement(GetInnerXml(el)),
                        Uid = cref.Substring(cref.IndexOf(':') + 1)
                    };
                }).ToList(),
                Parameters = dElement.Elements("param")?.Where(p => !string.IsNullOrEmpty(p.Attribute("name").Value)).ToDictionary(p => p.Attribute("name").Value, p => NormalizeDocsElement(GetInnerXml(p))),
                TypeParameters = dElement.Elements("typeparam")?.Where(p => !string.IsNullOrEmpty(p.Attribute("name").Value)).ToDictionary(p => p.Attribute("name").Value, p => NormalizeDocsElement(GetInnerXml(p))),
                Returns = NormalizeDocsElement(GetInnerXml(dElement.Element("returns"))),
                Since = NormalizeDocsElement(dElement.Element("since")?.Value),
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

        private static string NormalizeDocsElement(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Trim() == "To be added.")
            {
                return null;
            }
            return xrefFix.Replace(str.Trim(), m => "<" + m.Groups[1].Value + (m.Groups.Count == 3 && !string.IsNullOrEmpty(m.Groups[2].Value) ? "*" : "") + ">");
        }

        private AssemblyInfo ParseAssemblyInfo(XElement ele)
        {
            var assembly = new AssemblyInfo();
            assembly.Name = ele.Element("AssemblyName")?.Value;
            assembly.Versions = ele.Elements("AssemblyVersion").Select(v => v.Value).ToList();
            return assembly;
        }
    }
}
