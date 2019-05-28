using Microsoft.OpenPublishing.LogCodeService.Manager;
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
    public static class LogMessageUtility
    {
        private const string LogFolderRelativePath = @"Logging\";
        private const string LogFileRegex = @".+LogMessages\.json$";
        private static readonly LogCodeManager manager;

        static LogMessageUtility()
        {
            var path = Path.Combine(GetCurrentAssemblyFolder(), LogFolderRelativePath);
            manager = new LogCodeManager(path, LogFileRegex);
        }

        public static IEnumerable<string> GetLogCodes()
        {
            return from logCodeEntity in manager select logCodeEntity.LogCode;
        }

        public static string FormatMessage(LogCode logCode, params object[] args)
        {
            var logCodeEntity = manager.GetLogCodeEntity(logCode.ToString());
            if (string.IsNullOrEmpty(logCodeEntity?.Message))
            {
                //TraceEx.TraceError($"Cannot find message for log code {logCode}");
                return logCode.ToString();
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendFormat(logCodeEntity.Message, args);
                return sb.Length == 0 ? logCode.ToString() : sb.ToString();
            }
            catch (FormatException)
            {
                var argsString = args == null ? "<null>" : string.Join(", ", args);
                //TraceEx.TraceError($"Log message {logCodeEntity.Message} for log code {logCode} has fewer parameters than provided {argsString}");
                return $"{logCodeEntity.Message}:{argsString}";
            }
        }

        public static string GetCategory(string logCode)
        {
            var category = manager.GetCategory(logCode);
            return string.IsNullOrEmpty(category) ? "Default" : category;
        }

        public static string GetCurrentAssemblyFolder()
        {
            var assemblyUri = new Uri(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            return Path.GetDirectoryName(assemblyUri.LocalPath);
        }

        public static string GetDocumentUrl(string prefix, string category, string logCode)
        {
            if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(logCode))
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(manager.GetLogCodeEntity(logCode)?.DocumentUrl))
            {
                return null;
            }
            return LogCodeUtility.CreateDocumentUrlForLogCode(prefix, category, logCode);
        }
    }
}
