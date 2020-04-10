using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models
{
    public class UWPRequirements
    {
        [JsonProperty("deviceFamilies")]
        [YamlMember(Alias = "deviceFamilies")]
        public IEnumerable<DeviceFamily> DeviceFamilies { get; set; }

        [JsonProperty("apiContracts")]
        [YamlMember(Alias = "apiContracts")]
        public IEnumerable<APIContract> APIContracts { get; set; }

    }
}
