using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class SignatureModel
    {
        [JsonProperty("lang")]
        [YamlMember(Alias = "lang")]
        public string Lang { get; set; }

        [JsonProperty("value")]
        [YamlMember(Alias = "value")]
        public string Value { get; set; }
    }

    public class VersionedSignatureModel
    {
        [JsonProperty("lang")]
        [YamlMember(Alias = "lang")]
        public string Lang { get; set; }

        [JsonProperty("values")]
        [YamlMember(Alias = "values")]
        public List<VersionedValue> Values { get; set; }
    }
}
