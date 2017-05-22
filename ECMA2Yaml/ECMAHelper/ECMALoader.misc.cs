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
        private FilterStore LoadFilters(string path)
        {
            var filterFile = Path.Combine(path, "_filter.xml");
            if (File.Exists(filterFile))
            {
                XDocument filterDoc = XDocument.Load(filterFile);
                var attrFilterElements = filterDoc.Root.Element("attributeFilter")?.Elements("namespaceFilter");
                if (attrFilterElements != null)
                {
                    var filterStore = new FilterStore()
                    {
                        AttributeFilters = new List<IFilter>()
                    };
                    foreach (var fElement in attrFilterElements)
                    {
                        FullNameFilter filter = new FullNameFilter()
                        {
                            Namespace = fElement.Attribute("name").Value,
                            TypeFilters = new Dictionary<string, bool>(),
                            DefaultValue = true
                        };
                        foreach (var tFiler in fElement.Elements("typeFilter"))
                        {
                            bool expose = false;
                            bool.TryParse(tFiler.Attribute("expose").Value, out expose);
                            string name = tFiler.Attribute("name").Value;
                            if (name == "*")
                            {
                                filter.DefaultValue = expose;
                            }
                            else
                            {
                                filter.TypeFilters[name] = expose;
                            }
                        }
                        filterStore.AttributeFilters.Add(filter);
                    }

                    return filterStore;
                }
            }
            
            return null;
        }

        private Dictionary<string, List<string>> LoadFrameworks(string folder)
        {
            var frameworkFolder = Path.Combine(folder, "FrameworksIndex");
            if (!Directory.Exists(frameworkFolder))
            {
                return null;
            }
            Dictionary<string, List<string>> frameworks = new Dictionary<string, List<string>>();
            foreach (var fxFile in Directory.EnumerateFiles(frameworkFolder, "*.xml"))
            {
                XDocument fxDoc = XDocument.Load(fxFile);
                var fxName = fxDoc.Root.Attribute("Name").Value;
                foreach (var nsElement in fxDoc.Root.Elements("Namespace"))
                {
                    var ns = nsElement.Attribute("Name").Value;
                    frameworks.AddWithKey(ns, fxName);
                    foreach (var tElement in nsElement.Elements("Type"))
                    {
                        var t = tElement.Attribute("Id").Value;
                        frameworks.AddWithKey(t, fxName);
                        foreach (var mElement in tElement.Elements("Member"))
                        {
                            var m = mElement.Attribute("Id").Value;
                            frameworks.AddWithKey(m, fxName);
                        }
                    }
                }
            }

            return frameworks;
        }

        private Dictionary<string, string> LoadMonikerPackageMapping(string folder)
        {
            var file = Path.Combine(folder, "moniker2nuget.json");
            if (File.Exists(file))
            {
                try
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
                }
                catch(Exception ex)
                {
                    OPSLogger.LogUserError("Unable to load moniker to nuget mapping: " + ex.ToString(), file);
                    return null;
                }
            }
            return null;
        }

        private List<ExtensionMethod> LoadExtensionMethods(string path)
        {
            var indexFile = Path.Combine(path, "index.xml");
            if (!File.Exists(indexFile))
            {
                return null;
            }

            var extensionMethods = new List<ExtensionMethod>();
            XDocument idxDoc = XDocument.Load(indexFile);
            var emElements = idxDoc?.Root?.Element("ExtensionMethods")?.Elements("ExtensionMethod");
            if (emElements != null)
            {
                foreach(var em in emElements)
                {
                    extensionMethods.Add(new ExtensionMethod()
                    {
                        TargetDocId = em.Element("Targets").Element("Target").Attribute("Type").Value,
                        MemberDocId = em.Element("Member").Element("Link").Attribute("Member").Value,
                        ParentType = em.Element("Member").Element("Link").Attribute("Type").Value
                    });
                }
            }
            
            return extensionMethods;
        }
    }
}
