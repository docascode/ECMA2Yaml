using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public class DependencyItem
    {
        [JsonProperty("from_file_path")]
        public string FromFile { get; set; }
        [JsonProperty("to_file_path")]
        public string ToFile { get; set; }
        [JsonProperty("dependency_type")]
        public string DependencyType { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }

        public void TranslateFileNames(Dictionary<string, string> nameMapping)
        {
            if (nameMapping.TryGetValue(FromFile, out string newFromFile))
            {
                FromFile = newFromFile;
            }
            if (nameMapping.TryGetValue(ToFile, out string newToFile))
            {
                ToFile = newToFile;
            }
        }

        public string GetUniqueId()
        {
            return $"{FromFile}-{ToFile}-{DependencyType}-{Version}";
        }
    }
}
