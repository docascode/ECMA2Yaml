﻿using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models
{
    public class PerLanguageString
    {
        [JsonProperty("value")]
        [YamlMember(Alias = "value")]
        public string Value { get; set; }
        [JsonProperty("langs")]
        [YamlMember(Alias = "langs")]
        public HashSet<string> Langs { get; set; }

        public PerLanguageString() { }

        public PerLanguageString(HashSet<string> langs, string value)
        {
            Langs = langs;
            Value = value;
        }
    }

    public class VersionedString : VersionedValue<string>
    {
        public VersionedString() { }
        [JsonProperty("perLanguage")]
        [YamlMember(Alias = "perLanguage")]
        public List<PerLanguageString> PerLanguage { get; set; }
        public VersionedString(HashSet<string> monikers, string value) : base(monikers, value)
        {
        }
    }

    public class VersionedReturnType : VersionedString
    {
        [JsonProperty("refType")]
        [YamlMember(Alias = "refType")]
        public string RefType { get; set; }
        public VersionedReturnType() { }
        public VersionedReturnType(HashSet<string> monikers, string value, string reftype) : base(monikers, value)
        {
            RefType = reftype;
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

    public class VersionedCollection<T>
    {
        [JsonProperty("values")]
        [YamlMember(Alias = "values")]
        public List<T> Values { get; set; }
        [JsonProperty("valuesPerLanguage")]
        [YamlMember(Alias = "valuesPerLanguage")]
        public List<VersionedString> ValuesPerLanguage { get; set; }
        [JsonProperty("monikers")]
        [YamlMember(Alias = "monikers")]

        public HashSet<string> Monikers { get; set; }
        public VersionedCollection() { }

        public VersionedCollection(HashSet<string> monikers, List<T> value)
        {
            Monikers = monikers;
            Values = value;
        }
    }
}
