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
        public string LogFilePath = "log.json";
        public bool Flatten = false;

        List<string> Extras = null;

        OptionSet _options = null;

        public CommandLineOptions()
        {
            _options = new OptionSet {
                { "s|source=", "[Required] the folder path containing the mdoc generated xml files.", s => SourceFolder = s },
                { "o|output=", "[Required] the output folder to put yml files.", o => OutputFolder = o },
                { "m|metadata=", "the folder path containing the overwrite MD files for metadata.", s => MetadataFolder = s },
                { "l|log=", "the log file path.", l => LogFilePath = l },
                { "f|flatten", "to put all ymls in output root and not keep original folder structure.", f => Flatten = f != null },
                { "p|pathUrlMapping={=>}", "map local xml path to the Github url.", (p, u) => { RepoRootPath = p;  GitBaseUrl = u; } },
            };
        }

        public bool Parse(string[] args)
        {
            Extras = _options.Parse(args);
            if (string.IsNullOrEmpty(SourceFolder) || string.IsNullOrEmpty(OutputFolder))
            {
                OPSLogger.LogUserError("Invalid command line parameter.");
                Console.WriteLine("Usage: ECMA2Yaml.exe <Options>");
                _options.WriteOptionDescriptions(Console.Out);
                return false;
            }
            return true;
        }
    }
}
