using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ECMA2Yaml.Models
{
    public class Parameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string OriginalTypeString { get; set; }
        public string RefType { get; set; }
        public string Description { get; set; }
        public string Index { get; set; }
        public bool? IsContravariant { get; set; }
        public bool? IsCovariant { get; set; }
        public HashSet<string> Monikers { get; set; }

        public static Parameter FromXElement(XElement p)
        {
            if (p == null)
            {
                return null;
            }
            var typeStr = p.Attribute("Type")?.Value;
            var parameterAttributes = p.Element("Constraints")?.Elements("ParameterAttribute")?.ToArray();
            return new Parameter
            {
                Name = p.Attribute("Name")?.Value,
                Type = typeStr?.TrimEnd('&'),
                IsContravariant = parameterAttributes?.Any(pa => pa.Value == "Contravariant") == true ? true : (bool?)null,
                IsCovariant = parameterAttributes?.Any(pa => pa.Value == "Covariant") == true ? true : (bool?)null,
                OriginalTypeString = typeStr,
                RefType = p.Attribute("RefType")?.Value,
                Index = p.Attribute("Index")?.Value,
                Monikers = ECMALoader.LoadFrameworkAlternate(p)
            };
        }
    }
}
