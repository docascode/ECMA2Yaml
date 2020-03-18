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
        public ItemSDPModelBase()
        {
            Metadata = new Dictionary<string, object>();
        }

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

        [JsonProperty("nameWithType")]
        [YamlMember(Alias = "nameWithType")]
        public string NameWithType { get; set; }

        [JsonProperty("assemblies")]
        [YamlMember(Alias = "assemblies")]
        public IEnumerable<string> Assemblies { get; set; }

        [JsonProperty("attributes")]
        [YamlMember(Alias = "attributes")]
        public IEnumerable<string> Attributes { get; set; }

        [JsonProperty("attributesWithMoniker")]
        [YamlMember(Alias = "attributesWithMoniker")]
        public IEnumerable<VersionedString> AttributesWithMoniker { get; set; }

        [JsonProperty("attributeMonikers")]
        [YamlMember(Alias = "attributeMonikers")]
        public IEnumerable<string> AttributeMonikers { get; set; }

        [JsonProperty("syntax")]
        [YamlMember(Alias = "syntax")]
        public IEnumerable<SignatureModel> Syntax { get; set; }

        [JsonProperty("syntaxWithMoniker")]
        [YamlMember(Alias = "syntaxWithMoniker")]
        public IEnumerable<VersionedSignatureModel> SyntaxWithMoniker { get; set; }

        [JsonProperty("devLangs")]
        [YamlMember(Alias = "devLangs")]
        public IEnumerable<string> DevLangs { get; set; }

        [JsonProperty("monikers")]
        [YamlMember(Alias = "monikers")]
        public IEnumerable<string> Monikers { get; set; }

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

        [JsonProperty("sdk_requirements")]
        [YamlMember(Alias = "sdk_requirements")]
        public SDKRequirements SDKRequirements { get; set; }

        [JsonProperty("os_requirements")]
        [YamlMember(Alias = "os_requirements")]
        public OSRequirements OSRequirements{ get; set; }

        [JsonProperty("deviceFamilies")]
        [YamlMember(Alias = "deviceFamilies")]
        public IEnumerable<DeviceFamily> DeviceFamilies { get; set; }

        [JsonProperty("apiContracts")]
        [YamlMember(Alias = "apiContracts")]
        public IEnumerable<APIContract> ApiContracts { get; set; }

        [JsonProperty("capabilities")]
        [YamlMember(Alias = "capabilities")]
        public IEnumerable<string> Capabilities { get; set; }

        [JsonProperty("xamlMemberSyntax")]
        [YamlMember(Alias = "xamlMemberSyntax")]
        public string XamlMemberSyntax { get; set; }

        [JsonProperty("metadata")]
        [YamlMember(Alias = "metadata")]
        public Dictionary<string, object> Metadata { get; set; }
    }
}
