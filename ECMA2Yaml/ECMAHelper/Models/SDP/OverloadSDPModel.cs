using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class OverloadSDPModel : ItemSDPModelBase
    {
        [JsonIgnore]
        [YamlIgnore]
        public override string YamlMime => "YamlMime:NetMember";

        [JsonProperty("type")]
        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [JsonProperty("members")]
        [YamlMember(Alias = "members")]
        public IEnumerable<MemberSDPModel> Members { get; set; }
    }
}
