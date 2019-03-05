using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        public NamespaceSDPModel FormatNamespace(Namespace nsItem)
        {
            var sdpNS = InitWithBasicProperties<NamespaceSDPModel>(nsItem);

            if (nsItem.Types != null)
            {
                foreach(var tGroup in nsItem.Types?.GroupBy(t => t.ItemType))
                {
                    switch (tGroup.Key)
                    {
                        case ItemType.Class:
                            sdpNS.Classes = tGroup.Select(t => t.Uid).ToList();
                            break;
                        case ItemType.Delegate:
                            sdpNS.Delegates = tGroup.Select(t => t.Uid).ToList();
                            break;
                        case ItemType.Interface:
                            sdpNS.Interfaces = tGroup.Select(t => t.Uid).ToList();
                            break;
                        case ItemType.Struct:
                            sdpNS.Structs = tGroup.Select(t => t.Uid).ToList();
                            break;
                        case ItemType.Enum:
                            sdpNS.Enums = tGroup.Select(t => t.Uid).ToList();
                            break;
                    }
                }
            }

            return sdpNS;
        }
    }
}
