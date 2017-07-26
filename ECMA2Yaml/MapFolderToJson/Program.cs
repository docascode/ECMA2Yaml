using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MapFolderToJson
{
    class Program
    {
        static string currectFolder = null;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(CurrentDomain_ReflectionOnlyAssemblyResolve);
            if (Directory.Exists(args[0]))
            {
                List<Tuple<string, string>> MonikerAssemblyPairs = new List<Tuple<string, string>>();
                foreach(var monikerFolder in Directory.GetDirectories(args[0]))
                {
                    currectFolder = monikerFolder;
                    var monikerName = Path.GetFileName(monikerFolder);
                    foreach(var dll in Directory.GetFiles(monikerFolder, "*.dll"))
                    {
                        bool isFacade = false;
                        try
                        {
                            var asm = Assembly.ReflectionOnlyLoadFrom(dll);
                            isFacade = !asm.DefinedTypes.Any();
                        }
                        catch
                        {
                            //don't do any thing, real facade dll won't cause this error
                        }
                        if (!isFacade)
                        {
                            MonikerAssemblyPairs.Add(Tuple.Create(monikerName, Path.GetFileNameWithoutExtension(dll)));
                        }
                    }
                }
                var moniker2Assembly = MonikerAssemblyPairs.GroupBy(t => t.Item1).ToDictionary(g => g.Key, g => g.Select(t => t.Item2).ToArray());
                //var moniker2Assembly = MonikerAssemblyPairs.ToLookup(t => t.Item1, t => t.Item2);
                File.WriteAllText("_moniker2Assembly.json", JsonConvert.SerializeObject(moniker2Assembly, Formatting.Indented));
            }
        }

        static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                return Assembly.ReflectionOnlyLoad(args.Name);
            }
            catch(Exception ex)
            {
                var dllName = string.Format("{0}\\{1}.dll", currectFolder, args.Name.Split(',')[0]);
                if (File.Exists(dllName))
                {
                    return Assembly.ReflectionOnlyLoadFrom(dllName);
                }
            }

            return null;
        }
    }
}
