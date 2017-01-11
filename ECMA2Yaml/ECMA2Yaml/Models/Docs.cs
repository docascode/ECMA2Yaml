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
    }
}
