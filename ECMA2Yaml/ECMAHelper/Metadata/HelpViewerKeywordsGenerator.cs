using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public static class HelpViewerKeywordsGenerator
    {
        internal const string DotNetProductSuffix = "[.NET]";

        public static void Generate(ItemSDPModelBase model, ReflectionItem item)
        {
            if (model != null && !model.Metadata.ContainsKey(OPSMetadata.HelpViewerKeywords))
            {
                var keywords = GetHelpViewerKeywordsCore(model, item).ToList();
                if (keywords.Count > 0)
                {
                    model.Metadata[OPSMetadata.HelpViewerKeywords] = keywords;
                }
            }
        }

        private static IEnumerable<string> GetHelpViewerKeywordsCore(ItemSDPModelBase model, ReflectionItem item)
        {
            switch (item.ItemType)
            {
                case ItemType.Namespace:
                case ItemType.Class:
                case ItemType.Struct:
                case ItemType.Enum:
                case ItemType.Interface:
                case ItemType.Delegate:
                    yield return $"{model.FullName} {ConverterHelper.ItemTypeNameMapping[item.ItemType]} {DotNetProductSuffix}";
                    break;
                case ItemType.Constructor:
                    var c = item as Member;
                    yield return $"{c.DisplayName} {ConverterHelper.ItemTypeNameMapping[c.Parent.ItemType]} {DotNetProductSuffix}, constructors";
                    break;
                case ItemType.Method:
                case ItemType.Property:
                case ItemType.Field:
                case ItemType.Event:
                case ItemType.Operator:
                    var m = item as Member;
                    var itemTypeStr = ConverterHelper.ItemTypeNameMapping[item.ItemType];
                    if (m.IsEII)
                    {
                        yield return $"{item.Name} explicitly implemented {itemTypeStr} {DotNetProductSuffix}";
                    }
                    else if (!(item.Parent.ItemType == ItemType.Enum))
                    {
                        itemTypeStr = m.IsExtensionMethod ? $"extension method" : itemTypeStr;
                        yield return $"{m.Parent.Name}.{m.DisplayName} {itemTypeStr} {DotNetProductSuffix}";
                        yield return $"{m.DisplayName} {itemTypeStr} {DotNetProductSuffix}, {ConverterHelper.ItemTypeNameMapping[m.Parent.ItemType]} {m.Parent.Name}";
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
