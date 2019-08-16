using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiffFiles
{
    public class CommandLineOptions
    {
        public string OldPath { get; set; }
        public string NewPath { get; set; }
        public string LogPath { get; set; }
        public bool IsDiffPath { get; set; }

        List<string> Extras = null;
        OptionSet _options = null;

        public CommandLineOptions()
        {
            _options = new OptionSet {
                { "o|oldpath=", "[Required] the old file path.", s => OldPath = s },
                { "n|newpath=", "[Required] the new file path.", s => NewPath = s },
                { "l|logpath=", "[Required] the log file path.", s => LogPath = s },
                { "Path", "is diff path ",  s => IsDiffPath = s != null },
            };
        }

        public bool Parse(string[] args)
        {
            Extras = _options.Parse(args);
            if (string.IsNullOrEmpty(OldPath) || string.IsNullOrEmpty(NewPath) || string.IsNullOrEmpty(LogPath))
            {
                PrintUsage();
                return false;
            }
            return true;
        }

        private void PrintUsage()
        {
            Console.WriteLine("Usage: DiffFiles.exe <Options>");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }
}
