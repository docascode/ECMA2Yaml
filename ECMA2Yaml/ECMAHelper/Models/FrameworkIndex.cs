using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    [Serializable]
    public class FrameworkIndex
    {
        public Dictionary<string, List<string>> DocIdToFrameworkDict { get; set; }

        public Dictionary<string, Dictionary<string, AssemblyInfo>> FrameworkAssemblies { get; set; }

        public HashSet<string> AllFrameworks { get; set; }
    }
}
