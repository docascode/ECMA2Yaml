using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntellisenseFileGen.Models
{
    public class FrameworkIndex
    {
        //public Dictionary<string, List<string>> DocIdToFrameworkDict { get; set; }
        public Dictionary<string,List<FrameworkDocIdInfo>> NamespaceDocIdsDict { get; set; }
        public Dictionary<string, List<AssemblyInfo>> FrameworkAssemblies { get; set; }
    }

    public class FrameworkDocIdInfo
    {
        public string DocId { get; set; }
        public string ParentDocId { get; set; }
        public string Namespace { get; set; }
    }
}
