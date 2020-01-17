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
        [JsonProperty("remote")]
        [YamlMember(Alias = "remote")]
        public GitDetail Remote { get; set; }

        [JsonProperty("path")]
        [YamlMember(Alias = "path")]
        public string Path { get; set; }
    }

    public class GitDetail
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
