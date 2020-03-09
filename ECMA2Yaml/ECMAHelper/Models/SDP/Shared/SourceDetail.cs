using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class SourceDetail
    {
        [JsonProperty("path")]
        [YamlMember(Alias = "path")]
        public string RelativePath { get; set; }

        [JsonProperty("branch")]
        [YamlMember(Alias = "branch")]
        public string RemoteBranch { get; set; }

        [JsonProperty("repo")]
        [YamlMember(Alias = "repo")]
        public string RemoteRepositoryUrl { get; set; }
    }
}
