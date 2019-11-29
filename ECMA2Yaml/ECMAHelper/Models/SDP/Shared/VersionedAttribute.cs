using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class VersionedAttribute
    {
        [JsonProperty("uid")]
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }
        [JsonProperty("monikers")]
        [YamlMember(Alias = "monikers")]
        public string[] Monikers { get; set; }
    }
}
