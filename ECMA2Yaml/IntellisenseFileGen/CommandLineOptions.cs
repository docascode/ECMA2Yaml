using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntellisenseFileGen
{
    public class CommandLineOptions
    {
        public string DataRootPath { get; set; }
        public string OutFolder { get; set; }


        List<string> Extras = null;
        OptionSet _options = null;

        public CommandLineOptions()
        {
            _options = new OptionSet {
                { "r|rootpath=", "[Required] the data root path.", s => DataRootPath = s },
                { "o|outpath=", "[Required] output file path.",  s => OutFolder = s},
            };
        }

        public bool Parse(string[] args)
        {
            Extras = _options.Parse(args);
            if (string.IsNullOrEmpty(DataRootPath) || string.IsNullOrEmpty(OutFolder))
            {
                PrintUsage();
                return false;
            }
            return true;
        }

        private void PrintUsage()
        {
            Console.WriteLine("Usage: IntellisenseFileGen.exe <Options>");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }
}
