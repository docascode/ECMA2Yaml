using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    [Serializable]
    public class GitSourceDetail
    {
        public string RepoUrl { get; set; }
        public string RepoBranch { get; set; }
        public string Path { get; set; }
    }
}
