using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models
{
    [Serializable]
    public class ReturnValue
    {
        [JsonProperty("type")]
        [YamlMember(Alias = "type")]
        public IEnumerable<VersionedReturnType> VersionedTypes {get;set;}


        [JsonProperty("description")]
        [YamlMember(Alias = "description")]
        public string Description { get; set; }
    }
}