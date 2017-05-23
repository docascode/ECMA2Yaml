using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public class AttributeFilter
    {
        public string Namespace { get; set; }
        public Dictionary<string, bool> TypeFilters { get; set; }
        public bool DefaultValue { get; set; }

        public bool? Filter(Type t)
        {
            if (t.Parent.Name == Namespace)
            {
                if (TypeFilters.ContainsKey(t.Name))
                {
                    return TypeFilters[t.Name];
                }
                return DefaultValue;
            }
            return null;
        }
    }
}
