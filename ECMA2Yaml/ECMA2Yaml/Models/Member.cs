using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public enum MemberType
    {
        Constructor,
        Method,
        Property,
        Field
    }

    public class Member
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Type Parent { get; set; }
        public Dictionary<string, string> Signatures { get; set; }
        public AssemblyInfo AssemblyInfo { get; set; }
        public List<Parameter> TypeParameters { get; set; }
        public List<Parameter> Parameters { get; set; }
        public string ReturnValueType { get; set; }
        public Docs Docs { get; set; }
    }
}
