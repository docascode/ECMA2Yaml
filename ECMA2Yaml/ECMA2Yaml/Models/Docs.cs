using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ECMA2Yaml.Models
{
    //http://docs.go-mono.com/?link=man%3amdoc(5)
    public class Docs
    {
        public string Summary { get; set; }
        public string Remarks { get; set; }
        public List<XElement> AltMembers { get; set; }
        public XElement Exception { get; set; }
        public Dictionary<string, XElement> Parameters { get; set; }
        public Dictionary<string, XElement> TypeParameters { get; set; }
        public string Returns { get; set; }
        public string Since { get; set; }
        public string Value { get; set; }

        public static Docs FromXElement(XElement dElement)
        {
            if (dElement == null)
            {
                return null;
            }
            return new Docs()
            {
                Summary = dElement.Element("summary")?.Value,
                Remarks = dElement.Element("remarks")?.Value,
                AltMembers = dElement.Elements("altmember")?.ToList(),
                Exception = dElement.Element("exception"),
                Parameters = dElement.Elements("param")?.ToDictionary(p => p.Attribute("name").Value, p => p),
                TypeParameters = dElement.Elements("typeparam")?.ToDictionary(p => p.Attribute("name").Value, p => p),
                Returns = dElement.Element("returns")?.Value,
                Since = dElement.Element("since")?.Value,
                Value = dElement.Element("value")?.Value
            };

        }
    }
}
