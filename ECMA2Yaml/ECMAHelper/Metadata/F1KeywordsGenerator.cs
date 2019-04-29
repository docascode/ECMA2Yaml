using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public static class F1KeywordsGenerator
    {
        public static void Generate(ItemSDPModelBase model)
        {
            var keywords = GetF1Keywords(model).ToList();
            if (!model.Metadata.ContainsKey(OPSMetadata.F1Keywords))
            {
                model.Metadata[OPSMetadata.F1Keywords] = keywords;
            }
        }

        private static IEnumerable<string> GetF1Keywords(ItemSDPModelBase item)
        {
            var uid = item.Uid;
            if (uid == null)
            {
                yield break;
            }
            uid = uid.TrimEnd('*');
            var index = uid.IndexOf('(');
            if (index != -1)
            {
                uid = uid.Remove(index);
            }
            yield return uid;
            if (uid.Contains("."))
            {
                yield return uid.Replace(".", "::");
            }
        }
    }
}
