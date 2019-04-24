using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public abstract class ItemSDPModelBase
    {
        [JsonIgnore]
        [YamlIgnore]
        abstract public string YamlMime { get; }

        [JsonProperty("uid")]
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }

        [JsonProperty("commentId")]
        [YamlMember(Alias = "commentId")]
        public string CommentId { get; set; }

        [JsonProperty("namespace")]
        [YamlMember(Alias = "namespace")]
        public string Namespace { get; set; }

        [JsonProperty("name")]
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [JsonProperty("fullName")]
        [YamlMember(Alias = "fullName")]
        public string FullName { get; set; }

        [JsonProperty("assemblies")]
        [YamlMember(Alias = "assemblies")]
        public IEnumerable<string> Assemblies { get; set; }

        [JsonProperty("attributes")]
        [YamlMember(Alias = "attributes")]
        public IEnumerable<string> Attributes { get; set; }

        [JsonProperty("syntax")]
        [YamlMember(Alias = "syntax")]
        public IEnumerable<SignatureModel> Syntax { get; set; }

        [JsonProperty("devLangs")]
        [YamlMember(Alias = "devLangs")]
        public IEnumerable<string> DevLangs { get; set; }

        [JsonProperty("seeAlso")]
        [YamlMember(Alias = "seeAlso")]
        public string SeeAlso { get; set; }

        [JsonProperty("isDeprecated")]
        [YamlMember(Alias = "isDeprecated")]
        public bool IsDeprecated { get; set; }

        [JsonProperty("isInternalOnly")]
        [YamlMember(Alias = "isInternalOnly")]
        public bool IsInternalOnly { get; set; }

        [JsonProperty("additionalNotes")]
        [YamlMember(Alias = "additionalNotes")]
        public AdditionalNotes AdditionalNotes { get; set; }

        [JsonProperty("summary")]
        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [JsonProperty("remarks")]
        [YamlMember(Alias = "remarks")]
        public string Remarks { get; set; }

        [JsonProperty("examples")]
        [YamlMember(Alias = "examples")]
        public string Examples { get; set; }

        [JsonProperty("metadata")]
        [YamlMember(Alias = "metadata")]
        public Dictionary<string, object> Metadata { get; set; }
    }
}
