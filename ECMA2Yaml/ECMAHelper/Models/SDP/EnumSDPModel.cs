using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class EnumSDPModel : ItemSDPModelBase
    {
        public override string YamlMime { get; } = "YamlMime:NetEnum";

        [JsonProperty("inheritances")]
        [YamlMember(Alias = "inheritances")]
        public IEnumerable<string> Inheritances { get; set; }

        [JsonProperty("isFlags")]
        [YamlMember(Alias = "isFlags")]
        public bool IsFlags { get; set; }

        [JsonProperty("fields")]
        [YamlMember(Alias = "fields")]
        public IEnumerable<EnumField> Fields { get; set; }
    }
}
