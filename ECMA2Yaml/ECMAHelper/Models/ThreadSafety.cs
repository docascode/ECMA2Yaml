using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models
{
    public class ThreadSafety
    {
        [JsonProperty("customContent")]
        [YamlMember(Alias = "customContent")]
        public string CustomContent { get; set; }
        [JsonProperty("supported")]
        [YamlMember(Alias = "supported")]
        public bool? Supported { get; set; }
        [JsonProperty("memberScope")]
        [YamlMember(Alias = "memberScope")]
        public string MemberScope { get; set; }
    }
}
