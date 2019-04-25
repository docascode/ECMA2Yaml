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
        private Dictionary<string, string> MetadataMapping = new Dictionary<string, string>() {
            { OPSMetadata.OriginalContentUrl, OPSMetadata.SDP_op_overwriteFileGitUrl },
            { OPSMetadata.RefSkeletionUrl, OPSMetadata.SDP_op_articleFileGitUrl },
            { OPSMetadata.ContentUrl, OPSMetadata.ContentUrl }
        };

        private void MergeWhiteListedMetadata(ItemSDPModelBase model, ReflectionItem item)
        {
            if (item?.Metadata != null)
            {
                foreach(var pair in item.Metadata)
                {
                    if (MetadataMapping.TryGetValue(pair.Key, out string newKey))
                    {
                        model.Metadata[newKey] = pair.Value;
                    }
                }
            }
        }
    }
}
