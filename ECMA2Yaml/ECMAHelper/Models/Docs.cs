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
        public List<ExceptionDef> Exceptions { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public Dictionary<string, string> TypeParameters { get; set; }
        public string Returns { get; set; }
        public string Since { get; set; }
        public string Value { get; set; }
    }
}
