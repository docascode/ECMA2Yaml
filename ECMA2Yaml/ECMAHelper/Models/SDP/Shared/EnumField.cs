using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class EnumField
    {
        [JsonProperty("uid")]
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }

        [JsonProperty("commentId")]
        [YamlMember(Alias = "commentId")]
        public string CommentId { get; set; }

        [JsonProperty("literalValue")]
        [YamlMember(Alias = "literalValue")]
        public long LiteralValue { get; set; }

        [JsonProperty("name")]
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [JsonProperty("fullName")]
        [YamlMember(Alias = "fullName")]
        public string FullName { get; set; }

        [JsonProperty("summary")]
        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [JsonProperty("monikers")]
        [YamlMember(Alias = "monikers")]
        public IEnumerable<string> Monikers { get; set; }
    }
}
