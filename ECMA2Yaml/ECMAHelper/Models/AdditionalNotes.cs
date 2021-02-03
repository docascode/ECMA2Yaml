using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models
{
    [Serializable]
    public class AdditionalNotes
    {
        [JsonProperty("caller")]
        [YamlMember(Alias = "caller")]
        public string Caller { get; set; }
        [JsonProperty("implementer")]
        [YamlMember(Alias = "implementer")]
        public string Implementer { get; set; }
        [JsonProperty("inheritor")]
        [YamlMember(Alias = "inheritor")]
        public string Inheritor { get; set; }
    }
}
