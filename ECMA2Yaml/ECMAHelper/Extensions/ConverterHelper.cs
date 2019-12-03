using ECMA2Yaml.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            var contents = new SortedList<string, string>();
            if (item.Signatures?.Dict != null)
            {
                foreach (var sigPair in item.Signatures.Dict)
                {
                    if (Models.ECMADevLangs.OPSMapping.ContainsKey(sigPair.Key))
                    {
                        var lang = Models.ECMADevLangs.OPSMapping[sigPair.Key];
                        var val = sigPair.Value.LastOrDefault()?.Value;
                        if (sigPair.Key == ECMADevLangs.CSharp)
                        {
                            var contentBuilder = new StringBuilder();
                            if (item.Attributes?.Count > 0)
                            {
                                foreach (var att in item.Attributes.Where(attr => attr.Visible))
                                {
                                    contentBuilder.AppendFormat("[{0}]\n", att.Declaration);
                                }
                            }
                            contentBuilder.Append(val);
                            contents[lang] = contentBuilder.ToString();
                        }
                        else
                        {
                            contents[lang] = val;
                        }
                    }
                }
            }

            return contents;
        }

        public static SortedList<string, string> BuildUWPSignatures(ReflectionItem item)
        {
            var contents = new SortedList<string, string>();
            if (item.Signatures.Dict != null)
            {
                foreach (var sigPair in item.Signatures.Dict)
                {
                    if (Models.ECMADevLangs.OPSMapping.ContainsKey(sigPair.Key))
                    {
                        var langAlias = Models.ECMADevLangs.OPSMapping[sigPair.Key];
                        var val = sigPair.Value.LastOrDefault()?.Value;
                        switch (sigPair.Key)
                        {
                            case ECMADevLangs.CSharp:
                                contents[langAlias] = UWPCSharpSignatureTransform(val);
                                break;
                            case ECMADevLangs.CPP_CLI:
                            case ECMADevLangs.CPP_CX:
                            case ECMADevLangs.CPP_WINRT:
                                contents[langAlias] = UWPCPPSignatureTransform(val);
                                break;
                            default:
                                contents[langAlias] = val;
                                break;
                        }
                    }
                }
            }

            return contents;
        }

        private static readonly Regex CSharpSignatureLongNameRegex = new Regex("\\w+(\\.\\w+){2,}", RegexOptions.Compiled);
        private static string UWPCSharpSignatureTransform(string sig)
        {
            var csharpSyntax = CSharpSignatureLongNameRegex.Replace(sig, match =>
            {
                var val = match.Value;
                return val.Substring(val.LastIndexOf('.') + 1);
            });
            return csharpSyntax.Replace(" (", "(");
        }

        private static readonly Regex CPPSignatureLongNameRegex = new Regex("\\w+(::\\w+){2,}", RegexOptions.Compiled);
        private static string UWPCPPSignatureTransform(string sig)
        {
            return CPPSignatureLongNameRegex.Replace(sig, match =>
            {
                var val = match.Value;
                return val.Substring(val.LastIndexOf(':') + 1);
            });
        }
    }
}
