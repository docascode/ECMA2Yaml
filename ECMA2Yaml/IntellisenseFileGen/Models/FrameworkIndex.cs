using System.Collections.Generic;

namespace IntellisenseFileGen.Models
{
    public class FrameworkIndex
    {
        //public Dictionary<string, List<string>> DocIdToFrameworkDict { get; set; }
        public Dictionary<string, List<FrameworkDocIdInfo>> NamespaceDocIdsDict { get; set; }
        public Dictionary<string, List<AssemblyInfo>> FrameworkAssemblies { get; set; }
    }

    public class FrameworkDocIdInfo
    {
        public string DocId { get; set; }
        public string ParentDocId { get; set; }
        public string Namespace { get; set; }
    }
}
