using Microsoft.DocAsCode.Common;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    class Program
    {
        static void Main(string[] args)
        {
            WriteLine("ECMA2Yaml converter started.");

            string sourceFolder = null;
            string outputFolder = null;
            string logFilePath = "log.json";
            bool flatten = false;
            var options = new OptionSet {
                { "s|source=", "the folder path containing the mdoc generated xml files.", s => sourceFolder = s },
                { "o|output=", "the output folder to put yml files.", o => outputFolder = o },
                { "l|log=", "the log file path.", l => logFilePath = l },
                { "f|flatten", "to put all ymls in output root and not keep original folder structure.", f => flatten = f != null },
            };

            try
            {
                var extras = options.Parse(args);
                LoadAndConvert(sourceFolder, outputFolder, flatten);
            }
            catch(Exception ex)
            {
                WriteLine(ex.ToString());
                OPSLogger.LogSystemError(ex.ToString());
                Environment.ExitCode = -1;
            }
            finally
            {
                OPSLogger.Flush(logFilePath);
            }
        }

        static void LoadAndConvert(string sourceFolder, string outputFolder, bool flatten)
        {
            ECMALoader loader = new ECMALoader();
            WriteLine("Loading ECMAXML files...");
            var store = loader.LoadFolder(sourceFolder);
            if (store == null)
            {
                return;
            }

            WriteLine("Building loaded files...");
            store.Build();
            WriteLine("Loaded {0} namespaces.", store.Namespaces.Count);
            WriteLine("Loaded {0} types.", store.TypesByFullName.Count);
            WriteLine("Loaded {0} members.", store.MembersByUid.Count);

            WriteLine("Generating Yaml models...");
            var nsPages = TopicGenerator.GenerateNamespacePages(store);
            var typePages = TopicGenerator.GenerateTypePages(store);

            WriteLine("Writing Yaml files...");
            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.ForEach(nsPages, opt, nsPage =>
            {
                var nsFolder = Path.Combine(outputFolder, nsPage.Key);
                var nsFileName = Path.Combine(outputFolder, nsPage.Key + ".yml");
                YamlUtility.Serialize(nsFileName, nsPage.Value, YamlMime.ManagedReference);

                if (!flatten)
                {
                    if (!Directory.Exists(nsFolder))
                    {
                        Directory.CreateDirectory(nsFolder);
                    }
                }

                foreach (var t in store.Namespaces[nsPage.Key].Types)
                {
                    var typePage = typePages[t.Uid];
                    var fileName = Path.Combine(flatten ? outputFolder : nsFolder, t.Uid.Replace('`', '-') + ".yml");
                    YamlUtility.Serialize(fileName, typePage, YamlMime.ManagedReference);
                }
            });
            YamlUtility.Serialize(Path.Combine(outputFolder, "toc.yml"), TOCGenerator.Generate(store), YamlMime.TableOfContent);
            WriteLine("Done writing Yaml files.");
        }

        static void WriteLine(string format, params object[] args)
        {
            string timestamp = string.Format("[{0}]", DateTime.Now.ToString());
            Console.WriteLine(timestamp + string.Format(format, args));
        }
    }
}
