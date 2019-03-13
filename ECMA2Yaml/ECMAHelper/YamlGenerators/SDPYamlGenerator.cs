using ECMA2Yaml;
using ECMA2Yaml.Models;
using Microsoft.DocAsCode.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public static class SDPYamlGenerator
    {
        public static IDictionary<string, List<string>> Generate(
            ECMAStore store,
            string outputFolder,
            bool flatten)
        {
            WriteLine("Generating SDP Yaml models...");

            var sdpConverter = new SDPYamlConverter(store);
            sdpConverter.Convert();

            WriteLine("Writing SDP Yaml files...");
            ConcurrentDictionary<string, List<string>> fileMapping = new ConcurrentDictionary<string, List<string>>();
            ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.ForEach(store.Namespaces, po, ns =>
            {
                var nsFolder = Path.Combine(outputFolder, ns.Key);
                if (!string.IsNullOrEmpty(ns.Key) && sdpConverter.NamespacePages.TryGetValue(ns.Key, out var nsPage))
                {
                    var nsFileName = Path.Combine(outputFolder, ns.Key + ".yml");
                    if (!string.IsNullOrEmpty(ns.Value.SourceFileLocalPath))
                    {
                        fileMapping.TryAdd(ns.Value.SourceFileLocalPath, new List<string> { nsFileName });
                    }
                    YamlUtility.Serialize(nsFileName, nsPage, nsPage.YamlMime);
                }

                if (!flatten && !Directory.Exists(nsFolder))
                {
                    Directory.CreateDirectory(nsFolder);
                }

                foreach (var t in ns.Value.Types)
                {
                    if (!string.IsNullOrEmpty(t.Uid) && sdpConverter.TypePages.TryGetValue(t.Uid, out var typePage))
                    {
                        var tFileName = Path.Combine(flatten ? outputFolder : nsFolder, t.Uid.Replace('`', '-') + ".yml");
                        if (!string.IsNullOrEmpty(t.SourceFileLocalPath))
                        {
                            fileMapping.TryAdd(t.SourceFileLocalPath, new List<string> { tFileName });
                        }
                        YamlUtility.Serialize(tFileName, typePage, typePage.YamlMime);
                    }
                }
            });

            WriteLine("Done writing SDP Yaml files.");
            return fileMapping;
        }

        static void WriteLine(string format, params object[] args)
        {
            string timestamp = string.Format("[{0}]", DateTime.Now.ToString());
            Console.WriteLine(timestamp + string.Format(format, args));
        }
    }
}
