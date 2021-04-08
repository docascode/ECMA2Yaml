using ECMA2Yaml.IO;
using ECMA2Yaml.YamlHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ECMA2Yaml
{
    class Program
    {
        static void Main(string[] args)
        {
            var opt = new CommandLineOptions();

            try
            {
                if (opt.Parse(args))
                {
                    if (!string.IsNullOrEmpty(opt.RepoRootPath))
                    {
                        OPSLogger.PathTrimPrefix = opt.RepoRootPath;
                    }
                    if (!opt.SDPMode)
                    {
                        OPSLogger.LogUserError(LogCode.ECMA2Yaml_SDP_MigrationNeeded, ".openpublishing.publish.config.json");
                    }
                    else
                    {
                        LoadAndConvert(opt);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
                OPSLogger.LogSystemError(LogCode.ECMA2Yaml_InternalError, null, ex.ToString());
            }
            finally
            {
                OPSLogger.Flush(opt.LogFilePath);
            }
        }

        static void LoadAndConvert(CommandLineOptions opt)
        {
            var rootPath = Path.GetFullPath(opt.RepoRootPath ?? opt.SourceFolder);
            var xmlFolder = Path.GetFullPath(opt.SourceFolder).Replace(rootPath, "").Trim(Path.DirectorySeparatorChar);
            var fileAccessor = new FileAccessor(rootPath, opt.FallbackRepoRoot);
            ECMALoader loader = new ECMALoader(fileAccessor);
            WriteLine("Loading ECMAXML files...");
            var store = loader.LoadFolder(xmlFolder);
            if (store == null)
            {
                return;
            }
            store.StrictMode = opt.StrictMode;
            store.UWPMode = opt.UWPMode;
            store.DemoMode = opt.DemoMode;

            WriteLine("Building loaded files...");
            store.Build();
            if (OPSLogger.ErrorLogged)
            {
                return;
            }

            if (!string.IsNullOrEmpty(opt.RepoRootPath)
                && !string.IsNullOrEmpty(opt.PublicRepoUrl)
                && !string.IsNullOrEmpty(opt.PublicRepoBranch)
                && !string.IsNullOrEmpty(opt.RepoUrl)
                && !string.IsNullOrEmpty(opt.RepoBranch))
            {
                store.TranslateSourceLocation(opt.RepoRootPath, opt.RepoUrl, opt.RepoBranch, opt.PublicRepoUrl, opt.PublicRepoBranch);
            }
            else
            {
                WriteLine("Not enough information, unable to generate git url related metadata. -repoRoot {0}, -publicRepo {1}, -publicBranch {2}",
                    opt.RepoRootPath, opt.PublicRepoUrl, opt.PublicRepoBranch);
            }

            WriteLine("Loaded {0} namespaces.", store.Namespaces.Count);
            WriteLine("Loaded {0} types.", store.TypesByFullName.Count);
            WriteLine("Loaded {0} members.", store.MembersByUid.Count);
            WriteLine("Loaded {0} extension methods.", store.ExtensionMethodsByMemberDocId?.Values?.Count ?? 0);
            WriteLine("Loaded {0} attribute filters.", store.FilterStore?.AttributeFilters?.Count ?? 0);
            if (store.TypeMappingStore != null)
            {
                store.TypeMappingStore.LoadTypeXref(store);
            }
            
            if (!string.IsNullOrEmpty(opt.UndocumentedApiReport))
            {
                UndocumentedApi.ReportGenerator.GenerateReport(store, opt.UndocumentedApiReport.NormalizePath(), opt.RepoBranch);
            }

            IDictionary<string, List<string>> xmlYamlFileMapping = null;
            if (opt.SDPMode)
            {
                xmlYamlFileMapping = SDPYamlGenerator.Generate(store, opt.OutputFolder, opt.Flatten);
                YamlUtility.Serialize(Path.Combine(opt.OutputFolder, "toc.yml"), SDPTOCGenerator.Generate(store), "YamlMime:TableOfContent");
            }

            //Translate change list
            if (opt.ChangeListFiles.Count > 0)
            {
                foreach (var changeList in opt.ChangeListFiles)
                {
                    if (File.Exists(changeList))
                    {
                        var count = ChangeListUpdater.TranslateChangeList(changeList, xmlYamlFileMapping);
                        WriteLine("Translated {0} file entries in {1}.", count, changeList);
                    }
                }
            }
            WriteYamlXMLFileMap(opt.YamlXMLMappingFile, opt.RepoRootPath, xmlYamlFileMapping);

            //Save fallback file list as skip publish
            if (!string.IsNullOrEmpty(opt.SkipPublishFilePath) && loader.FallbackFiles?.Count > 0)
            {
                List<string> list = new List<string>();
                if (File.Exists(opt.SkipPublishFilePath))
                {
                    var jsonContent = File.ReadAllText(opt.SkipPublishFilePath);
                    list = JsonConvert.DeserializeObject<List<string>>(jsonContent);
                    WriteLine("Read {0} entries in {1}.", list.Count, opt.SkipPublishFilePath);
                }
                list.AddRange(loader.FallbackFiles
                    .Where(path => xmlYamlFileMapping.ContainsKey(path))
                    .SelectMany(path => xmlYamlFileMapping[path].Select(p => p.Replace(opt.RepoRootPath, "").TrimStart('\\')))
                    );
                var jsonText = JsonConvert.SerializeObject(list, Formatting.Indented);
                File.WriteAllText(opt.SkipPublishFilePath, jsonText);
                WriteLine("Write {0} entries to {1}.", list.Count, opt.SkipPublishFilePath);
            }

            WriteLine("Done writing Yaml files.");
        }

        /// <summary>
        /// save yaml -> xml mapping file, for build and localization to calculate file dependency
        /// </summary>
        /// <param name="fileMapPath"></param>
        /// <param name="repoRoot"></param>
        /// <param name="xmlYamlFileMap"></param>
        static void WriteYamlXMLFileMap(string fileMapPath, string repoRoot, IDictionary<string, List<string>> xmlYamlFileMap)
        {
            if (!string.IsNullOrEmpty(fileMapPath))
            {
                var yamlXMLMapping = new Dictionary<string, string>();
                if (File.Exists(fileMapPath))
                {
                    var existingMapContent = File.ReadAllText(fileMapPath);
                    if (!string.IsNullOrEmpty(existingMapContent))
                    {
                        yamlXMLMapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(existingMapContent);
                    }
                }

                char[] pathSplitters = new char[] { '\\', '/' };
                foreach (var singleXMLMapping in xmlYamlFileMap)
                {
                    var xmlFilePath = singleXMLMapping.Key;
                    if (!string.IsNullOrEmpty(repoRoot))
                    {
                        xmlFilePath = xmlFilePath.Replace(repoRoot, "").TrimStart(pathSplitters);
                    }
                    foreach (var yamlFile in singleXMLMapping.Value)
                    {
                        var mapKey = yamlFile;
                        if (!string.IsNullOrEmpty(repoRoot))
                        {
                            mapKey = mapKey.Replace(repoRoot, "").TrimStart(pathSplitters);
                        }
                        yamlXMLMapping[mapKey] = xmlFilePath;
                    }
                }
                var jsonText = JsonConvert.SerializeObject(yamlXMLMapping, Formatting.Indented);
                File.WriteAllText(fileMapPath, jsonText);
            }
        }

        static void WriteLine(string format, params object[] args)
        {
            string timestamp = string.Format("[{0}]", DateTime.Now.ToString());
            Console.WriteLine(timestamp + string.Format(format, args));
        }
    }
}
