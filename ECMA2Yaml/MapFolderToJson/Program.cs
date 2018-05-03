using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MapFolderToJson
{
    class Program
    {
        
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(AssemblyLoader.CurrentDomain_ReflectionOnlyAssemblyResolve);
            var settings = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
            };
            if (Directory.Exists(args[0]))
            {
                List<Tuple<string, string>> MonikerAssemblyPairs = new List<Tuple<string, string>>();
                foreach(var monikerFolder in Directory.GetDirectories(args[0]))
                {
                    var childDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, settings);

                    var handle = Activator.CreateInstance(childDomain,
                               typeof(AssemblyLoader).Assembly.FullName,
                               typeof(AssemblyLoader).FullName,
                               false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.CurrentCulture, new object[0]);

                    var loader = (AssemblyLoader)handle.Unwrap();

                    //This operation is executed in the new AppDomain
                    var paths = loader.LoadExceptFacade(monikerFolder);
                    MonikerAssemblyPairs.AddRange(paths);

                    AppDomain.Unload(childDomain);
                }
                var moniker2Assembly = MonikerAssemblyPairs.GroupBy(t => t.Item1).ToDictionary(g => g.Key, g => g.Select(t => t.Item2).ToArray());
                //var moniker2Assembly = MonikerAssemblyPairs.ToLookup(t => t.Item1, t => t.Item2);
                File.WriteAllText("_moniker2Assembly.json", JsonConvert.SerializeObject(moniker2Assembly, Formatting.Indented));
            }
        }
    }
}
