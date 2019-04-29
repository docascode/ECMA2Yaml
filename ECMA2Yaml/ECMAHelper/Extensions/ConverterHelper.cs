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
        public static readonly IReadOnlyDictionary<ItemType, string> ItemTypeNameMapping = new Dictionary<ItemType, string>()
        {
            {ItemType.Default, "default"},
            {ItemType.Toc, "toc"},
            {ItemType.Assembly, "assembly"},
            {ItemType.Namespace, "namespace"},
            {ItemType.Class, "class"},
            {ItemType.Interface, "interface"},
            {ItemType.Struct, "struct"},
            {ItemType.Delegate, "delegate"},
            {ItemType.Enum, "enum"},
            {ItemType.Field, "field"},
            {ItemType.Property, "property"},
            {ItemType.Event, "event"},
            {ItemType.Constructor, "constructor"},
            {ItemType.Method, "method"},
            {ItemType.Operator, "operator"},
            {ItemType.Container, "container"},
            {ItemType.AttachedEvent, "attachedevent"},
            {ItemType.AttachedProperty, "attachedproperty"}
        };

        public static SortedList<string, string> BuildSignatures(ReflectionItem item)
        {
            const string csharp = "C#";
            var contents = new SortedList<string, string>();
            if (item.Signatures != null)
            {
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
            }

            return contents;
        }
    }
}
