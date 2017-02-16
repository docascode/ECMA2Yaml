using ECMA2Yaml.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using Microsoft.DocAsCode.DataContracts.ManagedReference;

namespace ECMA2Yaml
{
    public class ECMALoader
    {
        private string _baseFolder;
        public ECMAStore LoadFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                return null;
            }

            _baseFolder = path;
            List<Namespace> namespaces = new List<Namespace>();
            foreach (var nsFile in Directory.EnumerateFiles(_baseFolder, "ns-*.xml"))
            {
                var nsFileName = Path.GetFileName(nsFile);
                var nsName = nsFileName.Substring("ns-".Length, nsFileName.Length - "ns-.xml".Length);
                if (!string.IsNullOrEmpty(nsName))
                {
                    namespaces.Add(LoadNamespace(nsFile));
                }
            }

            return new ECMAStore(namespaces, namespaces.SelectMany(ns => ns.Types));
        }

        private Namespace LoadNamespace(string nsFile)
        {
            XDocument nsDoc = XDocument.Load(nsFile);
            Namespace ns = new Namespace();
            ns.Id = ns.Name = nsDoc.Root.Attribute("Name").Value;
            ns.Types = LoadTypes(ns);
            ns.Docs = Docs.FromXElement(nsDoc.Root.Element("Docs"));

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
                    Console.Error.WriteLine("Error loading xml file {0}: {1}", typeFile, ex.ToString());
                    Environment.Exit(-1);
                }
            }
            return types;
        }

        private Models.Type LoadType(string typeFile)
        {
            string xmlContent = File.ReadAllText(typeFile);
            xmlContent = xmlContent.Replace("DefaultValue('&#x0;')</AttributeName>", "DefaultValue('\\0')</AttributeName>");
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
            t.Docs = Docs.FromXElement(tRoot.Element("Docs"));


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
            m.Docs = Docs.FromXElement(mElement.Element("Docs"));

            return m;
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
