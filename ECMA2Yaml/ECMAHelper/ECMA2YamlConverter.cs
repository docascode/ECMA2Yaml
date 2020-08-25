﻿using ECMA2Yaml.IO;
using ECMA2Yaml.YamlHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileAbstractLayer = ECMA2Yaml.IO.FileAbstractLayer;

namespace ECMA2Yaml
{
    public class ECMA2YamlConverter
    {
        public static void Run(
            string xmlDirectory,
            string outputDirectory,
            string fallbackXmlDirectory = null,
            string fallbackOutputDirectory = null,
            Action<LogItem> logWriter = null,
            string logContentBaseDirectory = null,
            string sourceMapFilePath = null,
            string repoUrl = null,
            string repoBranch = null,
            ECMA2YamlRepoConfig config = null)
        {
            if (xmlDirectory == null)
            {
                throw new ArgumentNullException(xmlDirectory);
            }
            if (!Directory.Exists(xmlDirectory))
            {
                throw new DirectoryNotFoundException($"{nameof(xmlDirectory)} {xmlDirectory} does not exist.");
            }
            if (outputDirectory == null)
            {
                throw new ArgumentNullException(outputDirectory);
            }
            if (!string.IsNullOrEmpty(fallbackXmlDirectory))
            {
                if (!Directory.Exists(fallbackXmlDirectory))
                {
                    throw new DirectoryNotFoundException($"{nameof(fallbackXmlDirectory)} {fallbackXmlDirectory} does not exist.");
                }
                if (string.IsNullOrEmpty(fallbackOutputDirectory))
                {
                    throw new ArgumentNullException(fallbackOutputDirectory,
                        $"{nameof(fallbackOutputDirectory)} cannot be empty if {nameof(fallbackXmlDirectory)} is present.");
                }
            }
            if (!string.IsNullOrEmpty(logContentBaseDirectory))
            {
                OPSLogger.PathTrimPrefix = logContentBaseDirectory.NormalizePath().AppendDirectorySeparator();
            }
            if (!string.IsNullOrEmpty(fallbackXmlDirectory))
            {
                OPSLogger.FallbackPathTrimPrefix = fallbackXmlDirectory.NormalizePath().AppendDirectorySeparator();
            }
            if (logWriter != null)
            {
                OPSLogger.WriteLogCallback = logWriter;
            }

            var fileAccessor = new FileAccessor(xmlDirectory, fallbackXmlDirectory);
            ECMALoader loader = new ECMALoader(fileAccessor);
            Console.WriteLine("Loading ECMAXML files...");
            var store = loader.LoadFolder("");
            if (store == null)
            {
                return;
            }

            Console.WriteLine("Building loaded files...");
            Console.WriteLine($"RepoUrl:{repoUrl}, repoBranch:{repoBranch}, ECMA2YamlRepoConfig:{JsonConvert.SerializeObject(config)}");
            store.UWPMode = config?.UWP ?? false;
            store.Build();

            if (store.UWPMode && !string.IsNullOrEmpty(repoUrl)
               && !string.IsNullOrEmpty(repoBranch))
            {
                store.TranlateContentSourceMeta(repoUrl, repoBranch);
            }
            else
            {
                Console.WriteLine("Not enough information, unable to generate git url related metadata. -publicRepo {0}, -publicBranch {1}",
                  repoUrl, repoBranch);
            }

            var xmlYamlFileMapping = SDPYamlGenerator.Generate(store, outputDirectory, flatten: config?.Flatten ?? true, withVersioning: true);
            if (loader.FallbackFiles != null && loader.FallbackFiles.Any() && !string.IsNullOrEmpty(fallbackOutputDirectory))
            {
                if (!Directory.Exists(fallbackOutputDirectory))
                {
                    Directory.CreateDirectory(fallbackOutputDirectory);
                }
                foreach (var fallbackFile in loader.FallbackFiles)
                {
                    if (xmlYamlFileMapping.TryGetValue(fallbackFile, out var originalYamls))
                    {
                        foreach(var originalYaml in originalYamls)
                        {
                            var newYaml = originalYaml.Replace(outputDirectory, fallbackOutputDirectory);
                            File.Copy(originalYaml, newYaml, overwrite: true);
                            File.Delete(originalYaml);
                        }
                        xmlYamlFileMapping.Remove(fallbackFile);
                    }
                }
            }
            if (!string.IsNullOrEmpty(sourceMapFilePath))
            {
                WriteYamlXMLFileMap(sourceMapFilePath, xmlYamlFileMapping);
            }

            var toc = SDPTOCGenerator.Generate(store);
            var tocOutputDirectory = string.IsNullOrEmpty(config?.BatchId) ? outputDirectory : Path.Combine(outputDirectory, config.BatchId);
            if (!Directory.Exists(tocOutputDirectory))
            {
                Directory.CreateDirectory(tocOutputDirectory);
            }
            YamlUtility.Serialize(Path.Combine(tocOutputDirectory, "toc.yml"), toc, "YamlMime:TableOfContent");
        }

        private static void WriteYamlXMLFileMap(string sourceMapFilePath, IDictionary<string, List<string>> xmlYamlFileMap)
        {
            if (!string.IsNullOrEmpty(sourceMapFilePath))
            {
                var yamlXMLMapping = new Dictionary<string, string>();

                foreach (var singleXMLMapping in xmlYamlFileMap)
                {
                    var xmlFilePath = FileAbstractLayer.RelativePath(singleXMLMapping.Key, sourceMapFilePath, true);
                    foreach (var yamlFile in singleXMLMapping.Value)
                    {
                        var mapKey = FileAbstractLayer.RelativePath(yamlFile, sourceMapFilePath, true);
                        yamlXMLMapping[mapKey] = xmlFilePath;
                    }
                }
                var json = JsonConvert.SerializeObject(new { files = yamlXMLMapping }, Formatting.Indented);
                File.WriteAllText(sourceMapFilePath, json);
            }
        }
    }
}
