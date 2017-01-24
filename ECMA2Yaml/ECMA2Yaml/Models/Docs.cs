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
        public string Examples { get; set; }
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

            var remarks = dElement.Element("remarks");
            string remarksText = null;
            string examplesText = null;
            if (remarks?.Element("format") != null)
            {
                remarksText = remarks.Element("format").Value;
            }
            else
            {
                remarksText = remarks?.Value;
            }
            if (remarksText != null)
            {
                remarksText = remarksText.Replace("## Remarks", "").Trim();
                if (remarksText.Contains("## Examples"))
                {
                    var pos = remarksText.IndexOf("## Examples");
                    examplesText = remarksText.Substring(pos).Trim();
                    remarksText = remarksText.Substring(0, pos).Trim();
                }
            }
            
            return new Docs()
            {
                Summary = dElement.Element("summary")?.Value,
                Remarks = remarksText,
                Examples = examplesText,
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
