using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models
{
    public class VersionedString : VersionedValue<string>
    {
        public VersionedString() { }

        public VersionedString(HashSet<string> monikers, string value) : base(monikers, value)
        {
        }
    }

    public class VersionedValue<T>
    {
        [JsonProperty("value")]
        [YamlMember(Alias = "value")]
        public T Value { get; set; }
        [JsonProperty("monikers")]
        [YamlMember(Alias = "monikers")]
        public HashSet<string> Monikers { get; set; }

        public VersionedValue() { }

        public VersionedValue(HashSet<string> monikers, T value)
        {
            Monikers = monikers;
            Value = value;
        }
    }
}
