using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        private HashSet<string> MetadataWhiteList = new HashSet<string>() {
            OPSMetadata.OriginalContentUrl,
            OPSMetadata.RefSkeletionUrl,
            OPSMetadata.ContentUrl
        };

        private void MergeWhiteListedMetadata(ItemSDPModelBase model, ReflectionItem item)
        {
            if (item?.Metadata != null)
            {
                if (model.Metadata == null)
                {
                    model.Metadata = new Dictionary<string, object>();
                }
                foreach(var pair in item.Metadata)
                {
                    if (MetadataWhiteList.Contains(pair.Key))
                    {
                        model.Metadata[pair.Key] = pair.Value;
                    }
                }
            }
        }
    }
}
