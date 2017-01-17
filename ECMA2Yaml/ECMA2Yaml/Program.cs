using Mono.Options;
using System;
using System.Collections.Generic;
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
            Console.WriteLine(store.Namespaces.Count);
            //store.TypesByFullName["System.Collections.Generic.Dictionary<TKey,TValue>"].Members.Select(m => m.Uid).ToList()
        }
    }
}
