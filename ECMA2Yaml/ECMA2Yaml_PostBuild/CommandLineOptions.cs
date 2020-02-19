﻿using Mono.Options;
using System;
using System.Collections.Generic;

namespace ECMA2Yaml
{
    public class CommandLineOptions
    {
        public string LogFilePath = "log.json";
        public string XMLYamlMappingFile = null;
        public string AggregatedDependencyFile = null;
        public List<string> Extras = null;

        private readonly OptionSet _options = null;

        public CommandLineOptions()
        {
            _options = new OptionSet {
                { "l|log=", "the log file path.", l => LogFilePath = l.NormalizePath() },
                { "xmlYamlMappingFile", "[Required] Mapping between XML files and generated yaml files",  s => XMLYamlMappingFile = s },
                { "fullDependencyFile", "[Required] The full dependency file generated by OPS",  s => AggregatedDependencyFile = s },
            };
        }

        public bool Parse(string[] args)
        {
            Extras = _options.Parse(args);
            if (string.IsNullOrEmpty(XMLYamlMappingFile) || string.IsNullOrEmpty(AggregatedDependencyFile))
            {
                PrintUsage();
                return false;
            }
            return true;
        }

        private void PrintUsage()
        {
            OPSLogger.LogUserError(LogCode.ECMA2Yaml_Command_Invalid, LogMessageUtility.FormatMessage(LogCode.ECMA2Yaml_Command_Invalid));
            Console.WriteLine("Usage: ECMA2Yaml_PostBuild.exe <Options>");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }
}
