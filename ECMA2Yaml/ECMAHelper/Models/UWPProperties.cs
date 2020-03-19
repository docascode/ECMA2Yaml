using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models
{
    public class UWPProperties
    {
        [JsonProperty("sdkRequirements")]
        [YamlMember(Alias = "sdkRequirements")]
        public SDKRequirements SDKRequirements { get; set; }

        [JsonProperty("osRequirements")]
        [YamlMember(Alias = "osRequirements")]
        public OSRequirements OSRequirements { get; set; }

        [JsonProperty("deviceFamilies")]
        [YamlMember(Alias = "deviceFamilies")]
        public IEnumerable<DeviceFamily> DeviceFamilies { get; set; }

        [JsonProperty("apiContracts")]
        [YamlMember(Alias = "apiContracts")]
        public IEnumerable<APIContract> APIContracts { get; set; }

        [JsonProperty("capabilities")]
        [YamlMember(Alias = "capabilities")]
        public IEnumerable<string> Capabilities { get; set; }
    }
}
