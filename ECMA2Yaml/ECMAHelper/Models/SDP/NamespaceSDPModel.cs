using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class NamespaceSDPModel : ItemSDPModelBase
    {
        public override string YamlMime { get; } = "YamlMime:NetNamespace";

        [JsonProperty("delegates")]
        [YamlMember(Alias = "delegates")]
        public IEnumerable<string> Delegates { get; set; }

        [JsonProperty("classes")]
        [YamlMember(Alias = "classes")]
        public IEnumerable<string> Classes { get; set; }

        [JsonProperty("enums")]
        [YamlMember(Alias = "enums")]
        public IEnumerable<string> Enums { get; set; }

        [JsonProperty("interfaces")]
        [YamlMember(Alias = "interfaces")]
        public IEnumerable<string> Interfaces { get; set; }

        [JsonProperty("structs")]
        [YamlMember(Alias = "structs")]
        public IEnumerable<string> Structs { get; set; }
    }
}
