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
        Field,
        Event
    }

    public class Member : ReflectionItem
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public MemberType Type { get; set; }
        public Dictionary<string, string> Signatures { get; set; }
        public AssemblyInfo AssemblyInfo { get; set; }
        public List<Parameter> TypeParameters { get; set; }
        public List<Parameter> Parameters { get; set; }
        public string ReturnValueType { get; set; }
        public Docs Docs { get; set; }

        public void BuildId(ECMAStore store)
        {
            Id = Type == MemberType.Constructor ? "#ctor" : Name;
            if (TypeParameters?.Count > 0)
            {
                Id += "``" + TypeParameters.Count;
            }
            if (Parameters?.Count > 0)
            {
                int genericCount = 0;
                List<string> ids = new List<string>();
                foreach (var p in Parameters)
                {
                    if (TypeParameters?.FirstOrDefault(tp => tp.Name == p.Type) != null)
                    {
                        ids.Add("``" + genericCount++);
                    }
                    else
                    {
                        ids.Add(store.TypesByFullName.ContainsKey(p.Type) ? store.TypesByFullName[p.Type].Uid : p.Type);
                    }
                }
                Id += string.Format("({0})", string.Join(",", ids));
            }
        }
    }
}
