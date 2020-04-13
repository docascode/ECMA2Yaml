using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public class PackageInfomarion
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string Feed { get; set; }
    }

    // moniker1
    //   |__ assembly1 => package1
    //   |__ assembly2 => package2
    public class PackageInfomarionMapping : Dictionary<string, Dictionary<string, PackageInfomarion>>
    {
        public PackageInfomarionMapping Merge(PackageInfomarionMapping pkgInfoMapping)
        {
            foreach (var kvp in pkgInfoMapping)
            {
                if (!this.ContainsKey(kvp.Key))
                {
                    this.Add(kvp.Key, kvp.Value);
                }
            }

            return this;
        }
    }
}
