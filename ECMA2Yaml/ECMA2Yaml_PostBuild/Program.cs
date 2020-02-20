using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    TranslateDependencyFile(opt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                OPSLogger.LogSystemError(LogCode.ECMA2Yaml_InternalError, ex.ToString());
            }
            finally
            {
                OPSLogger.Flush(opt.LogFilePath);
            }
        }

        static void TranslateDependencyFile(CommandLineOptions opt)
        {
            if (!File.Exists(opt.XMLYamlMappingFile))
            {
                OPSLogger.LogUserError(LogCode.ECMA2Yaml_File_LoadFailed, $"{opt.XMLYamlMappingFile} does not exist.");
            }
            if (!File.Exists(opt.AggregatedDependencyFile))
            {
                OPSLogger.LogUserError(LogCode.ECMA2Yaml_File_LoadFailed, $"{opt.AggregatedDependencyFile} does not exist.");
            }

            var xmlYamlMapping = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(opt.XMLYamlMappingFile));
            var yamlXMLMapping = new Dictionary<string, string>();
            foreach (var singleXMLMapping in xmlYamlMapping)
            {
                foreach (var yamlFile in singleXMLMapping.Value)
                {
                    yamlXMLMapping[yamlFile] = singleXMLMapping.Key;
                }
            }

            string line;
            var allDependencies = new List<DependencyItem>();
            HashSet<string> uniqueSet = new HashSet<string>();
            using (var file = new StreamReader(opt.AggregatedDependencyFile))
            {
                while (!string.IsNullOrWhiteSpace((line = file.ReadLine())))
                {
                    var item = JsonConvert.DeserializeObject<DependencyItem>(line);
                    item.TranslateFileNames(yamlXMLMapping);
                    var id = item.GetUniqueId();
                    if (uniqueSet.Add(id))
                    {
                        allDependencies.Add(item);
                    }
                }
            }
            var newLines = allDependencies.Select(item => JsonConvert.SerializeObject(item)).ToArray();
            File.WriteAllLines(opt.AggregatedDependencyFile, newLines);
        }
    }
}
