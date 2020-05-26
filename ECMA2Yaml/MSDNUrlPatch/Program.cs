using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDNUrlPatch
{
    class Program
    {
        static void Main(string[] args)
        {
            var opt = new CommandLineOptions();
            if (opt.Parse(args))
            {
                new UrlRepaireHelper().Start(opt.SourceFolder, opt.LogFilePath, opt.LogVerbose);
            }
        }
    }
}
