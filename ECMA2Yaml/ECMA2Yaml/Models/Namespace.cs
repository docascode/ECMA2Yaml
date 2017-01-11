using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public class Namespace
    {
        public string Name { get; set; }
        public List<Type> Types { get; set; }
        public Docs Docs { get; set; }
    }
}
