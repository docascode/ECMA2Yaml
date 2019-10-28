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
        public string InFolder { get; set; }
        public string OutFolder { get; set; }


        List<string> Extras = null;
        OptionSet _options = null;

        public CommandLineOptions()
        {
            _options = new OptionSet {
                { "i|inFolder=", "[Required] the in file folder.", s => InFolder = s },
                { "o|outFolder=", "[Required] the output file folder.",  s => OutFolder = s},
            };
        }

        public bool Parse(string[] args)
        {
            Extras = _options.Parse(args);
            if (string.IsNullOrEmpty(InFolder) || string.IsNullOrEmpty(OutFolder))
            {
                PrintUsage();
                return false;
            }
            return true;
        }

        private void PrintUsage()
        {
            Console.WriteLine("Usage: DiffXML.exe <Options>");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }
}
