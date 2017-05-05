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
        public bool StrictMode = false;

        public bool JoinTOC = false;
        public string TopLevelTOCPath = null;
        public string RefTOCPath = null;
        public string ConceptualTOCPath = null;
        public string ConceptualTOCUrl = null;
        public string RefTOCUrl = null;
        public bool HideEmptyNode = false;
        public string LandingPageMetadata;

        public bool FolderDiff = false;
        public string ChangeListPath = null;
        public string FolderToDiff = null;
        public string CacheFilePath = null;

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
                { "strict", "strict mode, means that any unresolved type reference will cause a warning",  s => StrictMode = s != null },

                { "joinTOC", "-joinTOC mode: join top level TOC with reference TOC by pattern matching",  j => JoinTOC = j != null },
                { "topLevelTOC=", "top level TOC file path, used in -joinTOC mode",  s => TopLevelTOCPath = s },
                { "refTOC=", "reference TOC file path, used in -joinTOC mode",  s => RefTOCPath = s },
                { "refTOCUrl=", "reference TOC published url, used in -joinTOC mode",  c => RefTOCUrl = c },
                { "conceptualTOC=", "conceptual TOC file path, used in -joinTOC mode",  c => ConceptualTOCPath = c },
                { "conceptualTOCUrl=", "conceptual TOC published url, used in -joinTOC mode",  c => ConceptualTOCUrl = c },
                { "hideEmptyNode", "hide a parent if its children pattern does not match any ref nodes, used in -joinTOC mode",  c => HideEmptyNode = c != null },
                { "landingPageMetadata=", "metadata json string, used in -joinTOC mode",  c => LandingPageMetadata = c },

                { "diffFolder", "-diffFolder mode: diff the files in folder with last build, and patch change list",  j => FolderDiff = j != null },
                { "changelistFile=", "change list file path, used in -diffFolder mode",  s => ChangeListPath = s },
                { "folderToDiff=", "folder to diff, used in -diffFolder mode",  s => FolderToDiff = s },
                { "cacheFile=", "cache file from last build, used in -diffFolder mode",  s => CacheFilePath = s },
            };
        }

        public bool Parse(string[] args)
        {
            Extras = _options.Parse(args);
            if (JoinTOC && (string.IsNullOrEmpty(TopLevelTOCPath) || string.IsNullOrEmpty(RefTOCPath)))
            {
                PrintUsage();
                return false;
            }
            if (FolderDiff && (string.IsNullOrEmpty(ChangeListPath) || string.IsNullOrEmpty(FolderToDiff) || string.IsNullOrEmpty(CacheFilePath)))
            {
                PrintUsage();
                return false;
            }
            if (!JoinTOC && !FolderDiff && (string.IsNullOrEmpty(SourceFolder) || string.IsNullOrEmpty(OutputFolder)))
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
