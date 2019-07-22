using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntellisenseFileGen.Models
{
    public class Type
    {
        public string DocId { get; set; }
        public List<string> AssemblyInfos { get;set;}
        public List<Member> Members { get; set; }
        public XElement Docs { get; set; }
    }
}
