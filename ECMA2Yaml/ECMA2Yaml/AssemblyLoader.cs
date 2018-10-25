using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    class AssemblyLoader : MarshalByRefObject
    {
        static string currectFolder = null;
        public List<Tuple<string, string>> LoadExceptFacade(string monikerFolder)
        {
            List<Tuple<string, string>> MonikerAssemblyPairs = new List<Tuple<string, string>>();
            currectFolder = monikerFolder;
            var monikerName = Path.GetFileName(monikerFolder);
            foreach (var dll in Directory.GetFiles(monikerFolder, "*.dll"))
            {
                bool isFacade = false;
                try
                {
                    var asm = Assembly.ReflectionOnlyLoadFrom(dll);
                    isFacade = !asm.DefinedTypes.Any();
                }
                catch (Exception ex)
                {
                    //don't do any thing, real facade dll won't cause this error
                }
                if (!isFacade)
                {
                    MonikerAssemblyPairs.Add(Tuple.Create(monikerName, Path.GetFileNameWithoutExtension(dll)));
                }
            }

            return MonikerAssemblyPairs;
        }
        public static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                return Assembly.ReflectionOnlyLoad(args.Name);
            }
            catch (Exception ex)
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
