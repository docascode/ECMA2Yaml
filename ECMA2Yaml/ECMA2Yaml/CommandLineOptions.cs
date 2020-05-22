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

        public string FallbackRepoRoot = null;
        public string RepoRootPath = null;
        public string RepoUrl = null;
        public string RepoBranch = null;
        public string PublicRepoUrl = null;
        public string PublicRepoBranch = null;

        public string SkipPublishFilePath = null;
        public string UndocumentedApiReport = null;
        public string YamlXMLMappingFile = null;
        public string LogFilePath = "log.json";
        public List<string> ChangeListFiles = new List<string>();
        public bool Flatten = false;
        public bool StrictMode = false;
        public bool SDPMode = false;
        public bool UWPMode = false;
        public bool DemoMode = false;
        public bool Versioning = true;
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
                { "SDP", "SDP mode, generate yamls in the .NET SDP schema format",  s => SDPMode = s != null },
                { "UWP", "UWP mode, special treatment for UWP pipeline",  s => UWPMode = s != null },
                { "demo", "demo mode, only for generating test yamls, do not set --SDP or --UWP together with this option.",  s => DemoMode = s != null },
                { "NoVersioning", "No-Versioning mode, don't output property-level versioning data",  s => Versioning = false },
                { "changeList=", "OPS change list file, ECMA2Yaml will translate xml path to yml path",  s => ChangeListFiles.Add(s)},
                { "skipPublishFilePath=", "Pass a file to OPS to let it know which files should skip publish",  s => SkipPublishFilePath = s.NormalizePath()},
                { "undocumentedApiReport=", "Save the Undocumented API validation result to Excel file",  s => UndocumentedApiReport = s.NormalizePath()},
                { "publicRepoBranch=", "the branch that is public to contributors", s => PublicRepoBranch = s},
                { "publicRepoUrl=", "the repo that is public to contributors", s => PublicRepoUrl = s},
                { "repoRoot=", "the local path of the root of the repo", s => RepoRootPath = s},
                { "fallbackRepoRoot=", "the local path of the root of the fallback repo", s => FallbackRepoRoot = s},
                { "repoUrl=", "the url of the current repo being processed", s => RepoUrl = s},
                { "repoBranch=", "the branch of the current repo being processed", s => RepoBranch = s},
                { "yamlXMLMappingFile=", "Mapping from generated yaml files to source XML files",  s => YamlXMLMappingFile = s },
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

            (var repoRootPath, var fallbackRepoRoot) = ECMALoader.GetRepoRootBySubPath(SourceFolder);
            if (string.IsNullOrEmpty(RepoRootPath))
            {
                RepoRootPath = repoRootPath;
            }
            if (string.IsNullOrEmpty(FallbackRepoRoot))
            {
                FallbackRepoRoot = fallbackRepoRoot;
            }
            if (DemoMode)
            {
                SDPMode = true;
                UWPMode = false;
            }
            PublicRepoBranch = PublicRepoBranch ?? RepoBranch;
            PublicRepoUrl = PublicRepoUrl ?? RepoUrl;

            PublicRepoUrl = NormalizeRepoUrl(PublicRepoUrl);
            RepoUrl = NormalizeRepoUrl(RepoUrl);
            return true;
        }

        private void PrintUsage()
        {
            OPSLogger.LogUserError(LogCode.ECMA2Yaml_Command_Invalid, null);
            Console.WriteLine("Usage: ECMA2Yaml.exe <Options>");
            _options.WriteOptionDescriptions(Console.Out);
        }

        private string NormalizeRepoUrl(string repoUrl)
        {
            if (!string.IsNullOrEmpty(repoUrl))
            {
                repoUrl = repoUrl.TrimEnd('/');
                if (repoUrl.EndsWith(".git"))
                {
                    repoUrl = repoUrl.Substring(0, repoUrl.Length - 4);
                }
            }
            return repoUrl;
        }
    }
}
