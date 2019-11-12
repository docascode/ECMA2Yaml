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
        public string SkipPublishFilePath = null;
        public string UndocumentedApiReport = null;
        public string LogFilePath = "log.json";
        public string CurrentBranch = null;
        public List<string> ChangeListFiles = new List<string>();
        public bool Flatten = false;
        public bool StrictMode = false;
        public bool MapMode = false;
        public bool SDPMode = false;
        public string PublicBranch = null;
        public string PublicRepoUrl = null;
        List<string> Extras = null;
        OptionSet _options = null;

        public CommandLineOptions()
        {
            _options = new OptionSet {
                { "s|source=", "[Required] the folder path containing the ECMAXML files.", s => SourceFolder = s.NormalizePath() },
                { "o|output=", "[Required] the output folder to put yml files.", o => OutputFolder = o.NormalizePath() },
                { "m|metadata=", "the folder path containing the overwrite MD files for metadata.", s => MetadataFolder = s.NormalizePath() },
                { "l|log=", "the log file path.", l => LogFilePath = l.NormalizePath() },
                { "f|flatten", "to put all ymls in output root and not keep original folder structure.", f => Flatten = f != null },
                { "strict", "strict mode, means that any unresolved type reference will cause a warning",  s => StrictMode = s != null },
                { "mapFolder", "folder mapping mode, maps assemblies in folder to json, used in .NET CI",  s => MapMode = s != null },
                { "SDP", "SDP mode, generate yamls in the .NET SDP schema format",  s => SDPMode = s != null },
                { "changeList=", "OPS change list file, ECMA2Yaml will translate xml path to yml path",  s => ChangeListFiles.Add(s)},
                { "skipPublishFilePath=", "Pass a file to OPS to let it know which files should skip publish",  s => SkipPublishFilePath = s.NormalizePath()},
                { "undocumentedApiReport=", "Save the Undocumented API validation result to Excel file",  s => UndocumentedApiReport = s.NormalizePath()},
                { "branch=", "current branch", s => CurrentBranch = s},
                { "publicBranch=", "the branch that is public to contributors", s => PublicBranch = s},
                { "publicRepoUrl=", "the branch that is public to contributors", s => PublicRepoUrl = s},
                { "repoRoot=", "the local path of the root of the repo", s => RepoRootPath = s}
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
            if (string.IsNullOrEmpty(RepoRootPath))
            {
                RepoRootPath = ECMALoader.GetRepoRootBySubPath(SourceFolder);
            }
            PublicBranch = PublicBranch ?? CurrentBranch;
            return true;
        }

        private void PrintUsage()
        {
            OPSLogger.LogUserError(LogCode.ECMA2Yaml_Command_Invalid, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_Command_Invalid));
            Console.WriteLine("Usage: ECMA2Yaml.exe <Options>");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }
}
