using Microsoft.DocAsCode.DataContracts.ManagedReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public class Type : ReflectionItem
    {
        public string FullName { get; set; }
        public BaseType BaseType { get; set; }
        public MemberType MemberType { get; set; }
        public List<string> InheritanceUids { get; set; }
        public Dictionary<string, string> InheritedMembers { get; set; }
        public List<string> IsA { get; set; }
        public Dictionary<string, string> Signatures { get; set; }
        public AssemblyInfo AssemblyInfo { get; set; }
        public List<Parameter> TypeParameters { get; set; }
        public List<string> Interfaces { get; set; }
        public List<string> Attributes { get; set; }
        public List<Member> Members { get; set; }
        public List<Member> Overloads { get; set; }
        public Docs Docs { get; set; }

        public override void BuildId(ECMAStore store)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = Name.Replace('+', '.');
                if (TypeParameters?.Count > 0)
                {
                    var parts = Name.Split('<', '>');
                    if (parts.Length != 3)
                    {
                        throw new Exception("unknown generic type name: " + Name);
                    }
                    Id = parts[0] + '`' + TypeParameters.Count + parts[2];
                }
            }
        }
    }
}
