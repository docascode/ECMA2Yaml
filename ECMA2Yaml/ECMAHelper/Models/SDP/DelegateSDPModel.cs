using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class DelegateSDPModel : ItemSDPModelBase
    {
        public override string YamlMime { get; } = "YamlMime:NetDelegate";

        [JsonProperty("typeParameters")]
        [YamlMember(Alias = "typeParameters")]
        public IEnumerable<TypeParameter> TypeParameters { get; set; }

        [JsonProperty("returns")]
        [YamlMember(Alias = "returns")]
        public TypeReference Returns { get; set; }

        [JsonProperty("parameters")]
        [YamlMember(Alias = "parameters")]
        public IEnumerable<ParameterReference> Parameters { get; set; }
    }
}
