using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiffFiles
{
    public class CommandLine
    {
        [Option('o', "OldPath", Required = true, HelpText = "old file path")]
        public string OldPath { get; set; }

        [Option('n', "NewPath", Required = true, HelpText = "new file path")]
        public string NewPath { get; set; }

        [Option('l', "LogPath", Required = true, HelpText = "Compare result log path")]
        public string LogPath { get; set; }

        [Option("Path", Required = true, Default = true,  HelpText = "Compare two path")]
        public bool IsDiffPath { get; set; }
    }
}
