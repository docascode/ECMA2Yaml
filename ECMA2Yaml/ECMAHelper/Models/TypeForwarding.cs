using Lucene.Net.Store;
using System.Collections.Generic;

namespace ECMA2Yaml.Models
{
    public class TypeForwarding
    {
        public AssemblyInfo From { get; set; }
        public AssemblyInfo To { get; set; }
    }

    public class TypeForwardingChain
    {
        public Dictionary<string, List<TypeForwarding>> TypeForwardingsPerMoniker { get; set; }

        public TypeForwardingChain(List<VersionedValue<TypeForwarding>> fwds)
        {
            TypeForwardingsPerMoniker = new Dictionary<string, List<TypeForwarding>>();
            foreach (var fwd in fwds)
            {
                foreach (var moniker in fwd.Monikers)
                {
                    if (TypeForwardingsPerMoniker.TryGetValue(moniker, out var fwdList))
                    {
                        fwdList.Add(fwd.Value);
                    }
                    else
                    {
                        TypeForwardingsPerMoniker[moniker] = new List<TypeForwarding>() { fwd.Value };
                    }
                }
            }
        }
    }
}
