using ECMA2Yaml.IO;
using ECMA2Yaml.YamlHelpers;
using Newtonsoft.Json;
using System;
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
            if (!string.IsNullOrEmpty(logContentBaseDirectory))
            {
                OPSLogger.PathTrimPrefix = logContentBaseDirectory;
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
            if (!string.IsNullOrEmpty(sourceMapFilePath))
            {
                var translatedMapping = xmlYamlFileMapping.ToDictionary(
                    p => FileAbstractLayer.RelativePath(p.Key, sourceMapFilePath),
                    p => p.Value.Select(path => FileAbstractLayer.RelativePath(path, sourceMapFilePath)).ToList()
                    );
                var json = JsonConvert.SerializeObject(translatedMapping);
                File.WriteAllText(sourceMapFilePath, json);
            }

            var toc = SDPTOCGenerator.Generate(store);
            YamlUtility.Serialize(Path.Combine(outputDirectory, "toc.yml"), toc, "TableOfContent");
        }
    }
}
