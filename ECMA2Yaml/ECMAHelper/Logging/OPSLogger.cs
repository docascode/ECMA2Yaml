using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public class OPSLogger
    {
        public static string PathTrimPrefix = "";

        private static ConcurrentBag<LogItem> logBag = new ConcurrentBag<LogItem>();

        public static void LogUserError(string message, string file = null)
        {
            logBag.Add(new LogItem(message, "ECMA2Yaml", file, MessageSeverity.Error, LogItemType.User));
        }

        public static void LogUserWarning(string message, string file = null)
        {
            logBag.Add(new LogItem(message, "ECMA2Yaml", file, MessageSeverity.Warning, LogItemType.User));
        }

        public static void LogUserInfo(string message, string file = null)
        {
            logBag.Add(new LogItem(message, "ECMA2Yaml", file, MessageSeverity.Info, LogItemType.User));
        }

        public static void LogSystemError(string message, string file = null)
        {
            logBag.Add(new LogItem(message, "ECMA2Yaml", file, MessageSeverity.Error, LogItemType.System));
        }

        public static void Flush(string filePath)
        {
            if (logBag.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach(var log in logBag.ToArray())
                {
                    if (!string.IsNullOrEmpty(log.File) && !string.IsNullOrEmpty(PathTrimPrefix))
                    {
                        log.File = log.File.Replace(PathTrimPrefix, "");
                    }
                    var logStr = JsonConvert.SerializeObject(log);
                    sb.AppendLine(logStr);
                    if (log.MessageSeverity == MessageSeverity.Error)
                    {
                        Console.Error.WriteLine(logStr);
                        Environment.ExitCode = -1;
                    }
                }
                File.AppendAllText(filePath, sb.ToString());
            }
        }
    }
}
