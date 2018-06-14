using Mono.Options;
using System;
using System.Collections.Generic;

namespace ECMA2Yaml
{
    public class CommandLineOptions
    {
        public string SourceFolder = null;
        public string MetadataFolder = null;
        public string OutputFolder = null;
        public string RepoRootPath = null;
        public string GitBaseUrl = null;
        public string SkipPublishFilePath = null;
        public string UndocumentedApiReport = null;
        public string LogFilePath = "log.json";
        public string CurrentBranch = null;
        public List<string> ChangeListFiles = new List<string>();
        public bool Flatten = false;
        public bool StrictMode = false;
        public bool MapMode = false;
        
        List<string> Extras = null;
        OptionSet _options = null;

        public CommandLineOptions()
        {
            _options = new OptionSet {
                { "s|source=", "[Required] the folder path containing the ECMAXML files.", s => SourceFolder = s },
                { "o|output=", "[Required] the output folder to put yml files.", o => OutputFolder = o },
                { "m|metadata=", "the folder path containing the overwrite MD files for metadata.", s => MetadataFolder = s },
                { "l|log=", "the log file path.", l => LogFilePath = l },
                { "f|flatten", "to put all ymls in output root and not keep original folder structure.", f => Flatten = f != null },
                { "p|pathUrlMapping={=>}", "map local xml path to the Github url.", (p, u) => { RepoRootPath = p;  GitBaseUrl = u; } },
                { "strict", "strict mode, means that any unresolved type reference will cause a warning",  s => StrictMode = s != null },
                { "mapFolder", "folder mapping mode, maps assemblies in folder to json, used in .NET CI",  s => MapMode = s != null },
                { "changeList=", "OPS change list file, ECMA2Yaml will translate xml path to yml path",  s => ChangeListFiles.Add(s)},
                { "skipPublishFilePath=", "Pass a file to OPS to let it know which files should skip publish",  s => SkipPublishFilePath = s},
                { "undocumentedApiReport=", "Save the Undocumented API validation result to Excel file",  s => UndocumentedApiReport = s},
                { "branch=", "current branch", s => CurrentBranch = s}
            };
        }

        public bool Parse(string[] args)
        {
            Extras = _options.Parse(args);
            if (string.IsNullOrEmpty(SourceFolder) || string.IsNullOrEmpty(OutputFolder))
            {
                PrintUsage();
                return false;
            }
            return true;
        }

        private void PrintUsage()
        {
            OPSLogger.LogUserError("Invalid command line parameter.");
            Console.WriteLine("Usage: ECMA2Yaml.exe <Options>");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }
}
