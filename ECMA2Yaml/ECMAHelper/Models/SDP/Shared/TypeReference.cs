using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class TypeReference
    {
        [JsonProperty("description")]
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [JsonProperty("type")]
        [YamlMember(Alias = "type")]
        public string Type { get; set; }
    }
}
