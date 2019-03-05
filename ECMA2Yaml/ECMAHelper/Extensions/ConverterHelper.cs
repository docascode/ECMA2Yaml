using ECMA2Yaml.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public static class ConverterHelper
    {
        public static SortedList<string, string> BuildSignatures(ReflectionItem item)
        {
            const string csharp = "C#";
            var contents = new SortedList<string, string>();
            foreach (var sigPair in item.Signatures)
            {
                if (Models.Constants.DevLangMapping.ContainsKey(sigPair.Key))
                {
                    var lang = Models.Constants.DevLangMapping[sigPair.Key];
                    if (sigPair.Key == csharp)
                    {
                        var contentBuilder = new StringBuilder();
                        if (item.Attributes?.Count > 0)
                        {
                            foreach (var att in item.Attributes.Where(attr => attr.Visible))
                            {
                                contentBuilder.AppendFormat("[{0}]\n", att.Declaration);
                            }
                        }
                        contentBuilder.Append(sigPair.Value);
                        contents[lang] = contentBuilder.ToString();
                    }
                    else
                    {
                        contents[lang] = sigPair.Value;
                    }
                }
            }

            return contents;
        }
    }
}
