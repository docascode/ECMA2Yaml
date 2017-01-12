using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public class Type : ReflectionItem
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string BaseTypeName { get; set; }
        public Dictionary<string, string> Signatures { get; set; }
        public AssemblyInfo AssemblyInfo { get; set; }
        public List<Parameter> TypeParameters { get; set; }
        public List<string> Interfaces { get; set; }
        public List<Member> Members { get; set; }
        public Docs Docs { get; set; }
    }
}
