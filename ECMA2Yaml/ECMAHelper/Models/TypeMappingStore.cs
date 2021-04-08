using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECMA2Yaml.Models
{
    public class TypeMappingStore
    {
        public Dictionary<string, Dictionary<string, string>> TypeMappingPerLanguage { get; set; }

        // Store "From" and "To" attribute Type and it's xref dic
        // <TypeReplace From = "System.Object" To="CatLibrary.IAnimal" Langs="C++ CLI" />
        public Dictionary<string, string> ToTypeXrefDic { get; set; }
        public Dictionary<string, string> FromTypeXrefDic { get; set; }

        /// <summary>
        /// Do type mapping per language, then aggregate based on the mapped value
        /// </summary>
        /// <param name="typeString">original type string</param>
        /// <param name="totalLangs">HashSet of the total languages this repo supports</param>
        /// <returns></returns>
        public List<PerLanguageString> TranslateTypeString(
            string typeString,
            HashSet<string> totalLangs)
        {
            var defaultValue = new PerLanguageString() { Value = typeString, Langs = new HashSet<string>(totalLangs) };
            var rval = new List<PerLanguageString>() { defaultValue };
            if (TypeMappingPerLanguage?.Count > 0)
            {
                foreach (var mappingPerLang in TypeMappingPerLanguage)
                {
                    var lang = mappingPerLang.Key;
                    var mappingDict = mappingPerLang.Value;
                    var newTypeString = typeString;
                    if (totalLangs.Contains(lang))
                    {
                        foreach (var mapping in mappingDict)
                        {
                            if (newTypeString.Contains(mapping.Key))
                            {
                                string fromTypeXref = "";
                                string toTypeXref = "";

                                if (FromTypeXrefDic.ContainsKey(mapping.Key) && !string.IsNullOrEmpty(FromTypeXrefDic[mapping.Key]))
                                {
                                    fromTypeXref = FromTypeXrefDic[mapping.Key];
                                }
                                if (ToTypeXrefDic.ContainsKey(mapping.Value) && !string.IsNullOrEmpty(ToTypeXrefDic[mapping.Value]))
                                {
                                    toTypeXref = ToTypeXrefDic[mapping.Value];
                                }
                                
                                if(!string.IsNullOrEmpty(fromTypeXref) && !string.IsNullOrEmpty(toTypeXref))
                                {
                                    newTypeString = newTypeString.Replace(fromTypeXref, toTypeXref);
                                }
                                else
                                {
                                    newTypeString = newTypeString.Replace(mapping.Key, mapping.Value);
                                }
                            }
                        }
                        if (newTypeString != typeString)
                        {
                            rval.Add(new PerLanguageString() { Langs = new HashSet<string> { lang }, Value = newTypeString });
                            defaultValue.Langs.Remove(lang);
                        }
                        if (defaultValue.Langs.Count == 0)
                        {
                            rval.Remove(defaultValue);
                        }
                    }
                }
            }
            if (rval.Count > 2)
            {
                rval = rval.GroupBy(v => v.Value)
                    .Select(g => g.Count() == 1 ? g.First() : new PerLanguageString() { Value = g.Key, Langs = g.SelectMany(v => v.Langs).ToHashSet() })
                    .ToList();
            }
            else if (rval.Count == 1)
            {
                rval.First().Langs = null;
            }
            return rval;
        }

        public void LoadTypeXref(ECMAStore store)
        {
            if (this.FromTypeXrefDic != null && this.FromTypeXrefDic.Keys.Count() > 0)
            {
                this.FromTypeXrefDic.Keys.ToList().ForEach(key =>
                {
                    string xref = SDPYamlConverter.TypeStringToTypeMDString(key, store);
                    if (!string.IsNullOrEmpty(xref) && xref.StartsWith("<xref"))
                    {
                        this.FromTypeXrefDic[key] = xref;
                    }
                });
            }

            if (this.ToTypeXrefDic != null && this.ToTypeXrefDic.Keys.Count() > 0)
            {
                this.ToTypeXrefDic.Keys.ToList().ForEach(key =>
                {
                    string xref = SDPYamlConverter.TypeStringToTypeMDString(key, store);
                    if (!string.IsNullOrEmpty(xref) && xref.StartsWith("<xref"))
                    {
                        this.ToTypeXrefDic[key] = xref;
                    }
                });
            }
        }
    }
}
