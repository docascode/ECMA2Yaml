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
            var opt = new CommandLineOptions();

            try
            {
                if (opt.Parse(args))
                {
                    TranslateDependencyFile(opt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                OPSLogger.LogSystemError(LogCode.ECMA2Yaml_InternalError, ex.ToString());
            }
            finally
            {
                OPSLogger.Flush(opt.LogFilePath);
            }
        }

        static void TranslateDependencyFile(CommandLineOptions opt)
        {

        }
    }
}
