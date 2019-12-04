using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models
{
    public class VersionedValue
    {
        [JsonProperty("value")]
        [YamlMember(Alias = "value")]
        public string Value { get; set; }
        [JsonProperty("monikers")]
        [YamlMember(Alias = "monikers")]
        public string[] Monikers { get; set; }

        public VersionedValue() { }

        public VersionedValue(string[] monikers, string value)
        {
            Monikers = monikers;
            Value = value;
        }
    }
}
