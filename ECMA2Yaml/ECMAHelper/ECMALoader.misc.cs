﻿using ECMA2Yaml.IO;
using ECMA2Yaml.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Path = System.IO.Path;

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
                FrameworkAssemblies = new Dictionary<string, Dictionary<string, AssemblyInfo>>(),
                AllFrameworks = new HashSet<string>()
            };

            foreach (var fxFile in ListFiles(frameworkFolder, "*.xml").OrderBy(f => Path.GetFileNameWithoutExtension(f.AbsolutePath)))
            {
                XDocument fxDoc = XDocument.Load(fxFile.AbsolutePath);
                var fxName = fxDoc.Root.Attribute("Name").Value;
                frameworkIndex.AllFrameworks.Add(fxName);
                foreach (var nsElement in fxDoc.Root.Elements("Namespace"))
                {
                    var ns = nsElement.Attribute("Name").Value;
                    frameworkIndex.DocIdToFrameworkDict.AddWithKey("N:" + ns, fxName);
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

        private PackageInfomarionMapping LoadPackageInfomarionMapping(string folder)
        {
            var pkgInfoDir = Path.Combine(folder, "PackageInformation");
            var pkgInfoMapping = new PackageInfomarionMapping();

            foreach (var file in ListFiles(pkgInfoDir, Path.Combine(pkgInfoDir, "*.json")))
            {
                try
                {
                    var mapping = JsonConvert.DeserializeObject<PackageInfomarionMapping>(_fileAccessor.ReadAllText(file.AbsolutePath));
                    pkgInfoMapping.Merge(mapping);
                }
                catch (Exception ex)
                {
                    OPSLogger.LogUserError(LogCode.ECMA2Yaml_PackageInformation_LoadFailed, file.AbsolutePath, ex);
                }
            }
            return pkgInfoMapping;
        }

        private IEnumerable<FileItem> ListFiles(string subFolder, string wildCardPattern)
        {
            return _fileAccessor.ListFiles(wildCardPattern, subFolder);
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

        public static (string, string) GetRepoRootBySubPath(string path)
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
                    string fallbackPath = Path.Combine(path, "_repo.en-us");
                    if (!Directory.Exists(fallbackPath))
                    {
                        fallbackPath = null;
                    }

                    return (path, fallbackPath);
                }

                path = Path.GetDirectoryName(path);
            }
            return (null, null);
        }
    }
}
