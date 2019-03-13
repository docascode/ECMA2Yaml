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
        public static int TranslateChangeList(string changeListFile, IDictionary<string, List<string>> fileMapping)
        {
            int count = 0;
            var lines = File.ReadAllLines(changeListFile);
            var changeList = new Dictionary<string, string>();
            foreach (var l in lines)
            {
                if (!string.IsNullOrWhiteSpace(l))
                {
                    var parts = l.Split('\t');
                    var file = parts[0].Replace("/", "\\");
                    var change = parts[1].Trim();
                   
                    if (fileMapping != null && fileMapping.ContainsKey(file))
                    {
                        count++;
                        foreach (var yamlFile in fileMapping[file])
                        {
                            changeList[yamlFile] = change;
                        }
                    }
                    else
                    {
                        changeList[file] = change;
                    }
                }
            }

            var mappedFileName = changeListFile.Replace(".tsv", ".mapped.tsv");
            File.WriteAllLines(mappedFileName, changeList.Select(p => $"{p.Key}\t{p.Value}"));
            return count;
        }
    }
}
