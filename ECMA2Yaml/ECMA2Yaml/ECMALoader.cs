using ECMA2Yaml.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

namespace ECMA2Yaml
{
    public class ECMALoader
    {
        private string _baseFolder;
        public List<Namespace> LoadFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                return null;
            }

            _baseFolder = path;
            List<Namespace> namespaces = new List<Namespace>();
            foreach (var nsFile in Directory.EnumerateFiles(_baseFolder, "ns-*.xml"))
            {
                var nsName = nsFile.Substring("ns-".Length, nsFile.Length - "ns-.xml".Length);
                namespaces.Add(LoadNamespace(nsFile));
            }
            return namespaces;
        }

        private Namespace LoadNamespace(string nsFile)
        {
            XDocument nsDoc = XDocument.Load(nsFile);
            Namespace ns = new Namespace();
            ns.Name = nsDoc.Root.Attribute("Name").Value;
            ns.Types = LoadTypes(ns.Name);
            ns.Docs = LoadDocs(nsDoc.Root.Element("Docs"));

            return ns;
        }

        private List<Models.Type> LoadTypes(string nsName)
        {
            string nsFolder = Path.Combine(_baseFolder, nsName);
            if (!Directory.Exists(nsFolder))
            {
                return null;
            }
            List<Models.Type> types = new List<Models.Type>();
            foreach (var typeFile in Directory.EnumerateFiles(nsFolder, "*.xml"))
            {
                types.Add(LoadType(typeFile));
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
            t.AssemblyInfo.Version = assemblyElement.Element("AssemblyVersion").Value;

            //TypeParameters
            var tpElement = tRoot.Element("TypeParameters");
            if (tpElement != null)
            {
                t.TypeParameters = tpElement.Elements("TypeParameter").Select(tp => new Parameter() { Name = tp.Attribute("Name").Value }).ToList();
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
                t.Members = membersElement.Elements("Member").Select(m => LoadMember(m)).ToList();
            }

            //Docs
            t.Docs = LoadDocs(tRoot.Element("Docs"));
            return t;
        }

        private Member LoadMember(XElement mElement)
        {
            Member m = new Member();
            m.Name = mElement.Attribute("MemberName").Value;
            m.Type = mElement.Attribute("MemberType").Value;

            m.Signatures = new Dictionary<string, string>();
            foreach (var sig in mElement.Elements("MemberSignature"))
            {
                m.Signatures[sig.Attribute("Language").Value] = sig.Attribute("Value").Value;
            }

            var version = mElement.Element("AssemblyInfo")?.Element("AssemblyVersion").Value;
            m.AssemblyInfo = version != null ? new AssemblyInfo() { Version = version } : null;

            //TypeParameters
            var tpElement = mElement.Element("TypeParameters");
            if (tpElement != null)
            {
                m.TypeParameters = tpElement.Elements("TypeParameter").Select(tp => new Parameter() { Name = tp.Attribute("Name").Value }).ToList();
            }

            //Parameters
            var pElement = mElement.Element("Parameters");
            if (pElement != null)
            {
                m.Parameters = pElement.Elements("Parameter").Select(p => new Parameter() { Name = p.Attribute("Name").Value, Type = p.Attribute("Type").Value }).ToList();
            }

            m.ReturnValueType = mElement.Element("ReturnValue")?.Element("ReturnType")?.Value;

            //Docs
            m.Docs = LoadDocs(mElement.Element("Docs"));

            return m;
        }

        private Docs LoadDocs(XElement dElement)
        {
            if (dElement == null)
            {
                return null;
            }
            Docs docs = new Docs();
            docs.Summary = dElement.Element("summary")?.Value;
            docs.Remarks = dElement.Element("remarks")?.Value;
            docs.AltMembers = dElement.Elements("altmember")?.ToList();
            docs.Exception = dElement.Element("exception");
            docs.Parameters = dElement.Elements("param")?.ToDictionary(p => p.Attribute("name").Value, p => p);
            docs.TypeParameters = dElement.Elements("typeparam")?.ToDictionary(p => p.Attribute("name").Value, p => p);
            docs.Returns = dElement.Element("returns")?.Value;
            docs.Since = dElement.Element("since")?.Value;
            docs.Value = dElement.Element("value")?.Value;

            return docs;
        }
    }
}
