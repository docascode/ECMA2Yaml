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
            string sourceFolder = null;
            string outputFolder = null;
            var options = new OptionSet {
                { "s|source=", "the folder path containing the mdoc generated xml files.", s => sourceFolder = s },
                { "o|output=", "the output folder to put yml files.", o => outputFolder = o },
            };

            var extras = options.Parse(args);

            ECMALoader loader = new ECMALoader();
            Console.WriteLine("Loading ECMAXML files...");
            var store = loader.LoadFolder(sourceFolder);
            Console.WriteLine("Loaded {0} namespaces.", store.Namespaces.Count);
            Console.WriteLine("Loaded {0} types.", store.TypesByFullName.Count);

            Console.WriteLine("Generating Yaml models...");
            var nsPages = TopicGenerator.GenerateNamespacePages(store);
            var typePages = TopicGenerator.GenerateTypePages(store);

            Console.WriteLine("Writing Yaml files...");
            foreach (var nsPage in nsPages)
            { 
                YamlUtility.Serialize(nsPage.Key + ".yml", nsPage.Value, YamlMime.ManagedReference);

                if (!Directory.Exists(nsPage.Key))
                {
                    Directory.CreateDirectory(nsPage.Key);
                }
                foreach(var t in store.Namespaces[nsPage.Key].Types)
                {
                    var typePage = typePages[t.Uid];
                    var fileName = Path.Combine(nsPage.Key, t.Uid + ".yml");
                    YamlUtility.Serialize(fileName, typePage, YamlMime.ManagedReference);
                }
            }
        }
    }
}
