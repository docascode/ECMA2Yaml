using Mono.Options;
using System;
using System.Collections.Generic;

namespace IntellisenseFileGen
{
    public class CommandLineOptions
    {
        public string DocsetPath { get; set; }
        public string XmlPath { get; set; }
        public string OutFolder { get; set; }
        public string Moniker { get; set; }
        public string LogFilePath { get; set; }


        List<string> Extras = null;
        OptionSet _options = null;

        public CommandLineOptions()
        {
            _options = new OptionSet {
                { "d|docsetpath=", "[Required] the docset path.", s => DocsetPath = s },
                { "x|xmlpath=", "[Required] the xml data path.", s => XmlPath = s },
                { "o|outpath=", "[Required] output file path.",  s => OutFolder = s},
                { "m|moniker=", "moniker name.",  s => Moniker = s},
                { "l|log=", "the log file path.",  l => LogFilePath = l },
            };
        }

        public bool Parse(string[] args)
        {
            Extras = _options.Parse(args);
            if (string.IsNullOrEmpty(DocsetPath) || string.IsNullOrEmpty(XmlPath) || string.IsNullOrEmpty(OutFolder))
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
