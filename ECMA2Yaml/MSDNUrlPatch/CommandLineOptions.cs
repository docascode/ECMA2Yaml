using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDNUrlPatch
{
    public class CommandLineOptions
    {
        public string SourceFolder { get; set; }
        public string LogFilePath { get; set; }
        public bool LogVerbose { get; set; }
        public string BaseUrl { get; set; }
        public int BatchSize { get; set; }
        public string FileExtension { get; set; }
        public bool PreVersions { get; set; }
        public bool FixedVersions { get; set; }

        List<string> Extras = null;
        OptionSet _options = null;

        public CommandLineOptions()
        {
            _options = new OptionSet {
                { "s|source=", "[Required] the folder path containing the ECMAXML files.", s => SourceFolder = s },
                { "l|log=", "[Required] the log file path.",  l => LogFilePath = l },
                { "batchsize=", "[Required] how many files can be processed in one batch.",  l => BatchSize = int.Parse(l) },
                { "fileExtension=", "[Required] processed file extension name.",  l => FileExtension = l },    
                { "b|baseurl=", "Base url",  l => BaseUrl = l },
                { "Ver", "Log verbose",  s => LogVerbose = s != null },
                { "preVersions", "Is url contains 'previous-versions' need fix",  s => PreVersions = s != null },
                { "fixedVersions", "Is url contains 'view=' need fix",  s => FixedVersions = s != null },
            };
        }

        public bool Parse(string[] args)
        {
            Extras = _options.Parse(args);
            if (string.IsNullOrEmpty(SourceFolder) || string.IsNullOrEmpty(LogFilePath))
            {
                PrintUsage();
                return false;
            }
            return true;
        }

        private void PrintUsage()
        {
            Console.WriteLine("Usage: MSDNUrlPatch.exe <Options>");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }
}
