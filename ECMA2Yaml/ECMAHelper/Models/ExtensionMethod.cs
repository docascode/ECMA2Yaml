using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    [Serializable]
    public class ExtensionMethod
    {
        public string Uid { get; set; }
        public string MemberDocId { get; set; }
        public string TargetDocId { get; set; }
        public string ParentTypeString { get; set; }
        public ReflectionItem ParentType { get; set; }
    }
}
