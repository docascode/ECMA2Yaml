using System.Collections.Generic;
using System.Xml.Linq;

namespace IntellisenseFileGen.Models
{
    public class Type
    {
        public string DocId { get; set; }
        public string Uid { get; set; }
        public List<string> AssemblyInfos { get; set; }
        public List<Member> Members { get; set; }
        public XElement Docs { get; set; }
    }
}
