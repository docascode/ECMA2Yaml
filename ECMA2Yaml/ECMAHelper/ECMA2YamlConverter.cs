using ECMA2Yaml.IO;
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
            ECMA2YamlRepoConfig config = null)
        {
            if (xmlDirectory == null)
            {
                throw new ArgumentNullException(xmlDirectory);
            }
            if (outputDirectory == null)
            {
                throw new ArgumentNullException(outputDirectory);
            }
            if (!string.IsNullOrEmpty(fallbackXmlDirectory) && string.IsNullOrEmpty(fallbackOutputDirectory))
            {
                throw new ArgumentNullException(fallbackOutputDirectory,
                    $"{nameof(fallbackOutputDirectory)} cannot be empty if {nameof(fallbackXmlDirectory)} is present.");
            }
            if (!string.IsNullOrEmpty(logContentBaseDirectory))
            {
                OPSLogger.PathTrimPrefix = logContentBaseDirectory.NormalizePath().AppendDirectorySeparator();
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
            store.StrictMode = true;
            store.UWPMode = config?.UWP ?? false;
            store.Build();

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
                            File.Move(originalYaml, newYaml);
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
            YamlUtility.Serialize(Path.Combine(outputDirectory, "toc.yml"), toc, "YamlMime:TableOfContent");
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
