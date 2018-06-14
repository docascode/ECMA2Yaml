using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public class VersionedProperty<T>
    {
        public T Default { get; set; }
        public List<Tuple<T, List<string>>> VersionedValues { get; set; }
    }
}
