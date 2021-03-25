using System.Collections.Generic;
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
