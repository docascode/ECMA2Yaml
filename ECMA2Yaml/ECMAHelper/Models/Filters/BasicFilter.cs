using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ECMA2Yaml.Models
{
    public class BasicFilter
    {
        public string Name { get; set; } = "*";
        public Dictionary<string, bool> AttributeFilters { get; set; }
        public bool Expose { get; set; } = true;

        public BasicFilter(XElement element)
        {
            Name = element.Attribute("name").Value;
            Expose = bool.Parse(element.Attribute("expose")?.Value ?? "true");

            var attrFilterElements = element.Elements("attributeFilter");
            if (attrFilterElements != null)
            {
                AttributeFilters = new Dictionary<string, bool>();
                foreach (var attrElement in attrFilterElements)
                {
                    AttributeFilters.Add(attrElement.Attribute("name").Value, bool.Parse(attrElement.Attribute("expose")?.Value ?? "true"));
                }
            }
        }
    }
}
