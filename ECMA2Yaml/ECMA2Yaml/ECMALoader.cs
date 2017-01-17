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
                var t = LoadType(typeFile);
                t.Parent = ns;
                types.Add(t);
            }
            return types;
        }

        private Models.Type LoadType(string typeFile)
        {
            XDocument tDoc = XDocument.Load(typeFile);
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
            var assemblyElement = tRoot.Element("AssemblyInfo");
            t.AssemblyInfo = new AssemblyInfo();
            t.AssemblyInfo.Name = assemblyElement.Element("AssemblyName").Value;
            t.AssemblyInfo.Versions = assemblyElement.Elements("AssemblyVersion").Select(v => v.Value).ToList();

            //TypeParameters
            var tpElement = tRoot.Element("TypeParameters");
            if (tpElement != null)
            {
                t.TypeParameters = tpElement.Elements("TypeParameter")?.Select(tp => new Parameter() { Name = tp.Attribute("Name").Value }).ToList();
            }

            //BaseTypeName
            t.BaseTypeName = tRoot.Element("Base")?.Element("BaseTypeName")?.Value;

            //Interfaces
            var interfacesElement = tRoot.Element("Interfaces");
            if (interfacesElement != null)
            {
                t.Interfaces = interfacesElement.Elements("Interface").Select(i => i?.Element("InterfaceName")?.Value).ToList();
            }

            //Members
            var membersElement = tRoot.Element("Members");
            if (membersElement != null)
            {
                t.Members = membersElement.Elements("Member").Select(m =>
                {
                    var member = LoadMember(t, m);
                    member.FullName = t.FullName + "." + member.Name;
                    return member;
                }).ToList();
            }

            //Docs
            t.Docs = Docs.FromXElement(tRoot.Element("Docs"));

            return t;
        }

        private Member LoadMember(Models.Type t, XElement mElement)
        {
            Member m = new Member();
            m.Parent = t;
            m.Name = mElement.Attribute("MemberName").Value;
            m.MemberType = (MemberType)Enum.Parse(typeof(MemberType), mElement.Element("MemberType").Value);

            m.Signatures = new Dictionary<string, string>();
            foreach (var sig in mElement.Elements("MemberSignature"))
            {
                m.Signatures[sig.Attribute("Language").Value] = sig.Attribute("Value").Value;
            }

            var versions = mElement.Element("AssemblyInfo")?.Elements("AssemblyVersion").Select(v => v.Value).ToList();
            m.AssemblyInfo = versions != null ? new AssemblyInfo() { Versions = versions } : null;

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

            m.ReturnValueType = mElement.Element("ReturnValue")?.Element("ReturnType")?.Value;

            //Docs
            m.Docs = Docs.FromXElement(mElement.Element("Docs"));

            return m;
        }
    }
}
