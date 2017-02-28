using ECMA2Yaml.Models;
using Microsoft.DocAsCode.DataContracts.Common;
using Monodoc.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public static class EcmaDescExtensions
    {
        public static string ToDisplayName(this string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || !typeStr.Contains('<'))
            {
                var parts = typeStr.Split('.');
                return parts.Last();
            }

            return ECMAStore.GetOrAddTypeDescriptor(typeStr).ToDisplayName();
        }

        public static string ToSpecId(this string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || !typeStr.Contains('<'))
            {
                return typeStr;
            }
            return ECMAStore.GetOrAddTypeDescriptor(typeStr).ToSpecId();
        }

        public static string ToSpecId(this EcmaDesc desc)
        {
            if (desc == null)
            {
                return null;
            }
            var typeStr = string.IsNullOrEmpty(desc.Namespace) ? desc.TypeName : desc.Namespace + "." + desc.TypeName;
            if (desc.GenericTypeArgumentsCount > 0)
            {
                typeStr = string.Format("{0}{{{1}}}", typeStr, string.Join(",", desc.GenericTypeArguments.Select(d => d.ToSpecId())));
            }
            if (desc.ArrayDimensions?.Count > 0)
            {
                for (int i = 0; i < desc.ArrayDimensions?.Count; i++)
                {
                    typeStr = typeStr + "[]";
                }
            }
            if (desc.DescModifier == EcmaDesc.Mod.Pointer)
            {
                typeStr += "*";
            }
            return typeStr;
        }

        public static string ToOuterTypeUid(this EcmaDesc desc)
        {
            if (desc == null)
            {
                return null;
            }
            var typeStr = string.IsNullOrEmpty(desc.Namespace) ? desc.TypeName : (desc.Namespace + "." + desc.TypeName);
            if (desc.GenericTypeArgumentsCount > 0)
            {
                typeStr += "`" + desc.GenericTypeArgumentsCount;
            }

            return typeStr;
        }

        public static string ToDisplayName(this EcmaDesc desc)
        {
            if (desc == null)
            {
                return null;
            }

            string name = null;
            if (desc.GenericTypeArgumentsCount == 0)
            {
                name = desc.TypeName;
            }
            else
            {
                name = string.Format("{0}<{1}>", desc.TypeName, string.Join(",", desc.GenericTypeArguments.Select(d => d.ToDisplayName())));
            }
            if (desc.ArrayDimensions?.Count > 0)
            {
                for (int i = 0; i < desc.ArrayDimensions?.Count; i++)
                {
                    name += "[]";
                }
            }
            if (desc.DescModifier == EcmaDesc.Mod.Pointer)
            {
                name += "*";
            }

            return name;
        }

        public static List<SpecViewModel> ToSpecItems(this EcmaDesc desc)
        {
            List<SpecViewModel> list = new List<SpecViewModel>();
            list.Add(new SpecViewModel()
            {
                Name = desc.TypeName,
                NameWithType = desc.TypeName,
                FullName = desc.ToCompleteTypeName(),
                Uid = desc.ToOuterTypeUid()
            });

            if (desc.GenericTypeArgumentsCount > 0)
            {
                list.Add(new SpecViewModel()
                {
                    Name = "<",
                    NameWithType = "<",
                    FullName = "<"
                });

                list.AddRange(desc.GenericTypeArguments.First().ToSpecItems());
                for (int i = 1; i < desc.GenericTypeArgumentsCount; i++)
                {
                    list.Add(new SpecViewModel()
                    {
                        Name = ",",
                        NameWithType = ",",
                        FullName = ","
                    });
                    list.AddRange(desc.GenericTypeArguments[i].ToSpecItems());
                }

                list.Add(new SpecViewModel()
                {
                    Name = ">",
                    NameWithType = ">",
                    FullName = ">"
                });
            }

            if (desc.ArrayDimensions != null && desc.ArrayDimensions.Count > 0)
            {
                foreach (var arr in desc.ArrayDimensions)
                {
                    list.Add(new SpecViewModel()
                    {
                        Name = "[]",
                        NameWithType = "[]",
                        FullName = "[]"
                    });
                }
            }
            if (desc.DescModifier == EcmaDesc.Mod.Pointer)
            {
                list.Add(new SpecViewModel()
                {
                    Name = "*",
                    NameWithType = "*",
                    FullName = "*"
                });
            }

            return list;
        }

        public static void AddWithKeys(this Dictionary<string, List<string>> dict, string key1, string key2, string val)
        {
            var key = key1 + (string.IsNullOrEmpty(key2) ? "" : (" - " + key2));
            if (!dict.ContainsKey(key))
            {
                dict[key] = new List<string>();
            }
            dict[key].Add(val);
        }

        public static List<string> GetOrDefault(this Dictionary<string, List<string>> dict, string key1, string key2)
        {
            var key = key1 + (string.IsNullOrEmpty(key2) ? "" : (" - " + key2));
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return null;
        }
    }
}
