using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public class VersionedProperty<VT>
    {
        public Dictionary<string, VT> ValuesPerMoniker { get; private set; }
        public Dictionary<VT, List<string>> MonikersPerValue { get; private set; }

        public VersionedProperty(Dictionary<string, VT> valuesPerMoniker)
        {
            ValuesPerMoniker = valuesPerMoniker;
            MonikersPerValue = ValuesPerMoniker.GroupBy(pair => pair.Value).ToDictionary(g => g.Key, g => g.Select(p => p.Key).Distinct().ToList());
        }
    }
}
