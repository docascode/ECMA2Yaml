using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class ParameterReference : TypeReference
    {
        [JsonProperty("name")]
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [JsonProperty("namesWithMonikers")]
        [YamlMember(Alias = "namesWithMonikers")]
        public List<VersionedString> NamesWithMonikers { get; set; }
    }
}
