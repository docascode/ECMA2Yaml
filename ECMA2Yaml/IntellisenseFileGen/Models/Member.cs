
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntellisenseFileGen.Models
{
    public class Member
    {
        public string DocId { get; set; }
        public string CommentId { get; set; }
        public string Uid { get; set; }
        public XElement Docs { get; set; }
        public List<string> AssemblyInfos { get; set; }
    }
}
