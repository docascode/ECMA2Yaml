using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class MemberSDPModel : ItemSDPModelBase
    {
        public override string YamlMime { get; } = "YamlMime:NetMember";

        [JsonProperty("typeParameters")]
        [YamlMember(Alias = "typeParameters")]
        public IEnumerable<TypeParameterSDPModel> TypeParameters { get; set; }

        [JsonProperty("returns")]
        [YamlMember(Alias = "returns")]
        public TypeReference Returns { get; set; }

        [JsonProperty("returnsWithMoniker")]
        [YamlMember(Alias = "returnsWithMoniker")]
        public ReturnValue ReturnsWithMoniker { get; set; }

        [JsonProperty("parameters")]
        [YamlMember(Alias = "parameters")]
        public IEnumerable<ParameterReference> Parameters { get; set; }

        [JsonProperty("threadSafety")]
        [YamlMember(Alias = "threadSafety")]
        public ThreadSafety ThreadSafety { get; set; }

        [JsonProperty("permissions")]
        [YamlMember(Alias = "permissions")]
        public IEnumerable<TypeReference> Permissions { get; set; }

        [JsonProperty("exceptions")]
        [YamlMember(Alias = "exceptions")]
        public IEnumerable<TypeReference> Exceptions { get; set; }

        [JsonProperty("implements")]
        [YamlMember(Alias = "implements")]
        public IEnumerable<string> Implements { get; set; }

        [JsonProperty("isNotClsCompliant")]
        [YamlMember(Alias = "isNotClsCompliant")]
        public bool? IsNotClsCompliant { get; set; }

        [JsonProperty("altCompliant")]
        [YamlMember(Alias = "altCompliant")]
        public string AltCompliant { get; set; }
    }
}
