using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class TypeSDPModel : ItemSDPModelBase
    {
        public override string YamlMime { get; } = "YamlMime:NetType";

        [JsonProperty("typeParameters")]
        [YamlMember(Alias = "typeParameters")]
        public IEnumerable<TypeParameter> TypeParameters { get; set; }

        [JsonProperty("type")]
        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [JsonProperty("threadSafety")]
        [YamlMember(Alias = "threadSafety")]
        public ThreadSafety ThreadSafety { get; set; }

        [JsonProperty("permissions")]
        [YamlMember(Alias = "permissions")]
        public IEnumerable<TypeReference> Permissions { get; set; }

        [JsonProperty("implements")]
        [YamlMember(Alias = "implements")]
        public IEnumerable<string> Implements { get; set; }

        [JsonProperty("inheritances")]
        [YamlMember(Alias = "inheritances")]
        public IEnumerable<string> Inheritances { get; set; }

        [JsonProperty("derivedClasses")]
        [YamlMember(Alias = "derivedClasses")]
        public IEnumerable<string> DerivedClasses { get; set; }

        [JsonProperty("isNotClsCompliant")]
        [YamlMember(Alias = "isNotClsCompliant")]
        public bool? IsNotClsCompliant { get; set; }

        [JsonProperty("altCompliant")]
        [YamlMember(Alias = "altCompliant")]
        public string AltCompliant { get; set; }

        #region Children

        [JsonProperty("extensionMethods")]
        [YamlMember(Alias = "extensionMethods")]
        public IEnumerable<MemberReference> ExtensionMethods { get; set; }

        [JsonProperty("constructors")]
        [YamlMember(Alias = "constructors")]
        public IEnumerable<MemberReference> Constructors { get; set; }

        [JsonProperty("operators")]
        [YamlMember(Alias = "operators")]
        public IEnumerable<MemberReference> Operators { get; set; }

        [JsonProperty("methods")]
        [YamlMember(Alias = "methods")]
        public IEnumerable<MemberReference> Methods { get; set; }

        [JsonProperty("eiis")]
        [YamlMember(Alias = "eiis")]
        public IEnumerable<MemberReference> EIIs { get; set; }

        [JsonProperty("properties")]
        [YamlMember(Alias = "properties")]
        public IEnumerable<MemberReference> Properties { get; set; }

        [JsonProperty("events")]
        [YamlMember(Alias = "events")]
        public IEnumerable<MemberReference> Events { get; set; }

        [JsonProperty("fields")]
        [YamlMember(Alias = "fields")]
        public IEnumerable<MemberReference> Fields { get; set; }

        [JsonProperty("attachedEvents")]
        [YamlMember(Alias = "attachedEvents")]
        public IEnumerable<MemberReference> AttachedEvents { get; set; }

        [JsonProperty("attachedProperties")]
        [YamlMember(Alias = "attachedProperties")]
        public IEnumerable<MemberReference> AttachedProperties { get; set; }

        #endregion
    }
}
