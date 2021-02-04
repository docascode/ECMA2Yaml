using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public class Namespace : ReflectionItem
    {
        public List<Type> Types { get; set; }

        public override void Build(ECMAStore store)
        {
            Id = Name;
        }
    }
}
