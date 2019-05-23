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
        public static void Generate(ItemSDPModelBase model, ReflectionItem item, List<ReflectionItem> childrenItems)
        {
            if (!model.Metadata.ContainsKey(OPSMetadata.F1Keywords))
            {
                var keywords = GetF1Keywords(item).ToList();
                if (childrenItems != null)
                {
                    foreach (var child in childrenItems)
                    {
                        keywords.AddRange(GetF1Keywords(child));
                    }
                }
                model.Metadata[OPSMetadata.F1Keywords] = keywords.Distinct().ToList();
            }
        }

        private static IEnumerable<string> GetF1Keywords(ReflectionItem item)
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
