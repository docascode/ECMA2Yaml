using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    class ChangeListUpdater
    {
        public static int TranslateChangeList(string changeListFile, IDictionary<string, string> fileMapping)
        {
            if (fileMapping != null && fileMapping.Count > 0)
            {
                int count = 0;
                var lines = File.ReadAllLines(changeListFile);
                var mappedLines = new List<string>();
                foreach (var l in lines)
                {
                    if (!string.IsNullOrWhiteSpace(l))
                    {
                        var parts = l.Split('\t');
                        var file = parts[0].Replace("/", "\\");
                        if (fileMapping.ContainsKey(file))
                        {
                            parts[0] = fileMapping[file];
                            mappedLines.Add(string.Join("\t", parts));
                            count++;
                        }
                        else
                        {
                            mappedLines.Add(l);
                        }
                    }
                }

                var mappedFileName = changeListFile.Replace(".tsv", ".mapped.tsv");
                File.WriteAllLines(changeListFile, mappedLines);
                return count;
            }
            return 0;
        }
    }
}
