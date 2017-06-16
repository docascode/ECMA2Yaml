using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapFolderToJson
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Directory.Exists(args[0]))
            {
                List<Tuple<string, string>> MonikerAssemblyPairs = new List<Tuple<string, string>>();
                foreach(var monikerFolder in Directory.GetDirectories(args[0]))
                {
                    var monikerName = Path.GetFileName(monikerFolder);
                    foreach(var dll in Directory.GetFiles(monikerFolder, "*.dll"))
                    {
                        MonikerAssemblyPairs.Add(Tuple.Create(monikerName, Path.GetFileNameWithoutExtension(dll)));
                    }
                }
                var moniker2Assembly = MonikerAssemblyPairs.GroupBy(t => t.Item1).ToDictionary(g => g.Key, g => g.Select(t => t.Item2).ToArray());
                //var moniker2Assembly = MonikerAssemblyPairs.ToLookup(t => t.Item1, t => t.Item2);
                File.WriteAllText("_moniker2Assembly.json", JsonConvert.SerializeObject(moniker2Assembly, Formatting.Indented));
            }
        }
    }
}
