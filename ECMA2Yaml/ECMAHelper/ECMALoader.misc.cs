using ECMA2Yaml.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.OpenPublishing.FileAbstractLayer;
using Path = System.IO.Path;
using System.IO;

namespace ECMA2Yaml
{
    public partial class ECMALoader
    {
        private FilterStore LoadFilters(string path)
        {
            var filterFile = Path.Combine(path, "_filter.xml");
            if (_fileAccessor.Exists(filterFile))
            {
                var filterStore = new FilterStore();
                XDocument filterDoc = XDocument.Parse(_fileAccessor.ReadAllText(filterFile));
                var attrFilter = filterDoc.Root.Element("attributeFilter");
                if (attrFilter != null && attrFilter.Attribute("apply").Value == "true")
                {
                    var attrFilterElements = attrFilter.Elements("namespaceFilter");
                    if (attrFilterElements != null)
                    {
                        filterStore.AttributeFilters = new List<AttributeFilter>();
                        foreach (var fElement in attrFilterElements)
                        {
                            AttributeFilter filter = new AttributeFilter()
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
                    }
                }
                var apiFilter = filterDoc.Root.Element("apiFilter");
                if (apiFilter != null && apiFilter.Attribute("apply").Value == "true")
                {
                    var apiFilterElements = apiFilter.Elements("namespaceFilter");
                    if (apiFilterElements != null)
                    {
                        filterStore.TypeFilters = new List<TypeFilter>();
                        filterStore.MemberFilters = new List<MemberFilter>();
                        foreach (var fElement in apiFilterElements)
                        {
                            var nsName = fElement.Attribute("name").Value?.Trim();
                            foreach (var tElement in fElement.Elements("typeFilter"))
                            {
                                var tFilter = new TypeFilter(tElement)
                                {
                                    Namespace = nsName
                                };
                                filterStore.TypeFilters.Add(tFilter);

                                var memberFilterElements = tElement.Elements("memberFilter");
                                if (memberFilterElements != null)
                                {
                                    foreach (var mElement in memberFilterElements)
                                    {
                                        filterStore.MemberFilters.Add(new MemberFilter(mElement)
                                        {
                                            Parent = tFilter
                                        });
                                    }
                                }
                            }

                        }
                    }
                }
                return filterStore;
            }

            return null;
        }

        private FrameworkIndex LoadFrameworks(string folder)
        {
            var frameworkFolder = Path.Combine(folder, "FrameworksIndex");
            FrameworkIndex frameworkIndex = new FrameworkIndex()
            {
                DocIdToFrameworkDict = new Dictionary<string, List<string>>(),
                FrameworkAssemblies = new Dictionary<string, Dictionary<string, AssemblyInfo>>()
            };

            foreach (var fxFile in ListFiles(frameworkFolder, Path.Combine(frameworkFolder, "*.xml")).OrderBy(f => Path.GetFileNameWithoutExtension(f.AbsolutePath)))
            {
                XDocument fxDoc = XDocument.Load(fxFile.AbsolutePath);
                var fxName = fxDoc.Root.Attribute("Name").Value;
                foreach (var nsElement in fxDoc.Root.Elements("Namespace"))
                {
                    var ns = nsElement.Attribute("Name").Value;
                    frameworkIndex.DocIdToFrameworkDict.AddWithKey(ns, fxName);
                    foreach (var tElement in nsElement.Elements("Type"))
                    {
                        var t = tElement.Attribute("Id").Value;
                        frameworkIndex.DocIdToFrameworkDict.AddWithKey(t, fxName);
                        foreach (var mElement in tElement.Elements("Member"))
                        {
                            var m = mElement.Attribute("Id").Value;
                            frameworkIndex.DocIdToFrameworkDict.AddWithKey(m, fxName);
                        }
                    }
                }

                var assemblyNodes = fxDoc.Root.Element("Assemblies")?.Elements("Assembly")?.Select(ele => new AssemblyInfo()
                {
                    Name = ele.Attribute("Name")?.Value,
                    Version = ele.Attribute("Version")?.Value
                }).ToList();
                if (assemblyNodes != null)
                {
                    frameworkIndex.FrameworkAssemblies.Add(fxName, assemblyNodes.ToDictionary(a => a.Name, a => a));
                }
            }
            return frameworkIndex;
        }

        private Dictionary<string, string> LoadMonikerPackageMapping(string folder)
        {
            var file = Path.Combine(folder, "_moniker2nuget.json");
            if (_fileAccessor.Exists(file))
            {
                try
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(_fileAccessor.ReadAllText(file));
                }
                catch (Exception ex)
                {
                    OPSLogger.LogUserError(LogCode.ECMA2Yaml_MonikerToNuget_Failed, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_MonikerToNuget_Failed, ex.ToString()), file);
                    return null;
                }
            }
            return null;
        }

        private Dictionary<string, List<string>> LoadMonikerAssemblyMapping(string folder)
        {
            var file = Path.Combine(folder, "_moniker2Assembly.json");
            if (_fileAccessor.Exists(file))
            {
                try
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(_fileAccessor.ReadAllText(file));
                }
                catch (Exception ex)
                {
                    OPSLogger.LogUserError(LogCode.ECMA2Yaml_MonikerToAssembly_Failed, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_MonikerToAssembly_Failed, ex.ToString()), file);
                    return null;
                }
            }
            return null;
        }

        private List<ExtensionMethod> LoadExtensionMethods(string path)
        {
            var indexFile = Path.Combine(path, "index.xml");
            if (!_fileAccessor.Exists(indexFile))
            {
                return null;
            }

            var extensionMethods = new List<ExtensionMethod>();
            XDocument idxDoc = XDocument.Parse(_fileAccessor.ReadAllText(indexFile));
            var emElements = idxDoc?.Root?.Element("ExtensionMethods")?.Elements("ExtensionMethod");
            if (emElements != null)
            {
                foreach (var em in emElements)
                {
                    extensionMethods.Add(new ExtensionMethod()
                    {
                        TargetDocId = em.Element("Targets").Element("Target").Attribute("Type").Value,
                        MemberDocId = em.Element("Member").Element("Link").Attribute("Member").Value,
                        ParentTypeString = em.Element("Member").Element("Link").Attribute("Type").Value
                    });
                }
            }

            return extensionMethods;
        }

        private IEnumerable<FileItem> ListFiles(string subFolder, string glob)
        {
            return _fileAccessor.ListFiles(new string[] { glob }, subFolder: subFolder);
        }

        private List<AssemblyInfo> ParseAssemblyInfo(XElement ele)
        {
            var name = ele.Element("AssemblyName")?.Value;
            var versions = ele.Elements("AssemblyVersion").Select(v => v.Value).ToList();
            if (versions.Count > 0)
            {
                return versions.Select(v => new AssemblyInfo
                {
                    Name = name,
                    Version = v
                }).ToList();
            }
            // Hack here, because mdoc sometimes inserts empty version for member assemblies, https://github.com/mono/api-doc-tools/issues/399
            // In ECMAStore we'll try to fallback to parent type assembly versions
            return new List<AssemblyInfo>()
            {
                new AssemblyInfo
                {
                    Name = name
                }
            };
        }

        private SortedList<string, List<string>> ParseModifiersFromSignatures(Dictionary<string, string> sigs, ReflectionItem item = null)
        {
            if (sigs == null)
            {
                return null;
            }

            var modifiers = new SortedList<string, List<string>>();
            if (sigs.TryGetValue("C#", out string val))
            {
                var mods = new List<string>();

                if (item != null && item.ItemType == ItemType.AttachedProperty)
                {
                    if (val.Contains(" Get" + item.Name))
                    {
                        mods.Add("get");
                    }
                    if (val.Contains(" Set" + item.Name))
                    {
                        mods.Add("set");
                    }
                }
                else
                {
                    var startWithModifiers = new string[] { "public", "protected", "private" };
                    mods.AddRange(startWithModifiers.Where(m => val.StartsWith(m)));
                    var containsModifiers = new string[] { "abstract", "static", "const", "readonly", "sealed", "get;", "set;" };
                    mods.AddRange(containsModifiers.Where(m => val.Contains(" " + m + " ")).Select(m => m.Trim(';')));
                }

                if (mods.Any())
                {
                    modifiers.Add("csharp", mods);
                }
            }
            return modifiers;
        }

        private void LoadMetadata(ReflectionItem item, XElement rootElement)
        {
            var metadataElement = rootElement.Element("Metadata");
            if (metadataElement != null)
            {
                item.ExtendedMetadata = new Dictionary<string, object>();
                foreach (var g in metadataElement.Elements("Meta")
                    ?.ToLookup(x => x.Attribute("Name").Value, x => x.Attribute("Value").Value))
                {
                    if (UWPMetadata.Values.TryGetValue(g.Key, out var datatype))
                    {
                        switch(datatype)
                        {
                            case MetadataDataType.String:
                                item.Metadata.Add(g.Key, g.First());
                                break;
                            case MetadataDataType.StringArray:
                                item.Metadata.Add(g.Key, g.ToArray());
                                break;
                        }
                    }
                    else
                    {
                        item.ExtendedMetadata.Add(g.Key, g.Count() == 1 ? (object)g.First() : (object)g.ToArray());
                    }
                }
            }
        }

        private ECMAAttribute LoadAttribute(XElement attrElement)
        {
            return new ECMAAttribute()
            {
                Declaration = attrElement.Element("AttributeName").Value,
                Visible = true,
                Monikers = LoadFrameworkAlternate(attrElement)
            };
        }

        public static HashSet<string> LoadFrameworkAlternate(XElement element)
        {
            return element.Attribute("FrameworkAlternate")?.Value.Split(';').ToHashSet();
        }

        public static string GetRepoRootBySubPath(string path)
        {
            while (!string.IsNullOrEmpty(path))
            {
                //var docfxJsonPath = Path.Combine(path, "docfx.json");
                //if (File.Exists(docfxJsonPath))
                //{
                //    DocsetRootPath = path;
                //}
                var repoConfigPath = Path.Combine(path, ".openpublishing.publish.config.json");
                if (File.Exists(repoConfigPath))
                {
                    return path;
                }

                path = Path.GetDirectoryName(path);
            }
            return null;
        }
    }
}
