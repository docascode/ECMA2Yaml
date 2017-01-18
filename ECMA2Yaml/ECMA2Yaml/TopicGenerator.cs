using ECMA2Yaml.Models;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Microsoft.DocAsCode.DataContracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public class TopicGenerator
    {
        public Dictionary<string, PageViewModel> GenerateNamespacePages(ECMAStore store)
        {
            var rval = new Dictionary<string, PageViewModel>();

            foreach (var ns in store.Namespaces)
            {
                rval.Add(ns.Key, ns.Value.ToPageViewModel());
            }

            return rval;
        }
    }

    public static class ModalConversionExtensions
    {
        public static PageViewModel ToPageViewModel(this Namespace ns)
        {
            var pv = new PageViewModel();
            pv.Items = new List<ItemViewModel>()
            {
                ns.ToItemViewModel()
            };
            pv.References = ns.Types.Select(t => t.ToReferenceViewModel()).ToList();
            return pv;
        }

        public static ItemViewModel ToItemViewModel(this Namespace ns)
        {
            var item = new ItemViewModel()
            {
                Id = ns.Id,
                Uid = ns.Uid,
                Name = ns.Name,
                NameWithType = ns.Name,
                FullName = ns.Name,
                Type = Microsoft.DocAsCode.DataContracts.ManagedReference.MemberType.Namespace,
                Children = ns.Types.Select(t => t.Uid).ToList()
            };
            return item;
        }

        public static ReferenceViewModel ToReferenceViewModel(this Models.Type t)
        {
            return new ReferenceViewModel()
            {
                Uid = t.Uid,
                Parent = t.Parent.Uid,
                IsExternal = false,
                Name = t.Name,
                NameWithType = t.Name,
                FullName = t.FullName
            };
        }

        public static PageViewModel ToPageViewModel(this Models.Type t, ECMAStore store)
        {
            var pv = new PageViewModel();
            pv.Items = new List<ItemViewModel>();
            pv.Items.Add(t.ToItemViewModel());
            pv.Items.AddRange(t.Members.Select(m => m.ToItemViewModel()));
            pv.References = t.Members.SelectMany(m => m.ToReferenceViewModels(store)).ToList();
            if (t.Overloads?.Count > 0)
            {
                pv.References.AddRange(t.Overloads.Select(o => o.ToReferenceViewModel()));
            }
            return pv;
        }

        public static MemberType InferTypeOfType(Models.Type t)
        {
            var signature = t.Signatures["C#"];
            if (t.BaseType == null && signature.Contains(" interface "))
            {
                return MemberType.Interface;
            }
            else if ("System.Enum" == t.BaseType.Name && signature.Contains(" enum "))
            {
                return MemberType.Enum;
            }
            else if ("System.Delegate" == t.BaseType.Name && signature.Contains(" delegate "))
            {
                return MemberType.Delegate;
            }
            else if ("System.ValueType" == t.BaseType.Name && signature.Contains(" struct "))
            {
                return MemberType.Struct;
            }
            else if (signature.Contains(" class "))
            {
                return MemberType.Class;
            }
            else
            {
                throw new Exception("Unable to identify the type of Type " + t.Uid);
            }
        }

        public static ItemViewModel ToItemViewModel(this Models.Type t)
        {
            var item = new ItemViewModel()
            {
                Id = t.Id,
                Uid = t.Uid,
                Name = t.Name,
                NameWithType = t.Name,
                FullName = t.FullName,
                Type = InferTypeOfType(t),
                Children = t.Members?.Select(m => m.Uid).ToList(),
                Syntax = t.ToSyntaxDetailViewModel()
            };
            return item;
        }

        public static SyntaxDetailViewModel ToSyntaxDetailViewModel(this Models.Type t)
        {
            var contentBuilder = new StringBuilder();
            if (t.Attributes?.Count > 0)
            {
                foreach (var att in t.Attributes)
                {
                    contentBuilder.AppendLine(att);
                }
            }
            contentBuilder.Append(t.Signatures["C#"]);
            var content = contentBuilder.ToString();
            var syntax = new SyntaxDetailViewModel()
            {
                Content = content,
                ContentForCSharp = content,
                TypeParameters = t.TypeParameters?.Select(tp => tp.ToApiParameter()).ToList()
            };

            return syntax;
        }

        public static ItemViewModel ToItemViewModel(this Member m)
        {
            var t = ((Models.Type)m.Parent);
            var item = new ItemViewModel()
            {
                Id = m.Id,
                Uid = m.Uid,
                Name = m.Name,
                NameWithType = t.Name + '.' + m.Name,
                FullName = m.FullName,
                Type = m.MemberType,
                AssemblyNameList = new List<string>() { t.AssemblyInfo.Name },
                NamespaceName = t.Parent.Name,
                Overload = m.Overload,
                Syntax = m.ToSyntaxDetailViewModel(),
                IsExplicitInterfaceImplementation = m.Name.Contains('.')
            };
            return item;
        }

        public static SyntaxDetailViewModel ToSyntaxDetailViewModel(this Member m)
        {
            var syntax = new SyntaxDetailViewModel()
            {
                Content = m.Signatures["C#"],
                ContentForCSharp = m.Signatures["C#"],
                Parameters = m.Parameters?.Select(p => p.ToApiParameter()).ToList(),
                Return = string.IsNullOrEmpty(m.ReturnValueType) ? new ApiParameter()
                {
                    Type = ToSpecId(m.ReturnValueType),
                    Description = "Return description to be filled"
                } : null
            };

            return syntax;
        }

        public static ApiParameter ToApiParameter(this Parameter p)
        {
            var ap = new ApiParameter()
            {
                Name = p.Name,
                Type = ToSpecId(p.Type),
                Description = "Parameter description to be filled"
            };

            return ap;
        }

        public static ReferenceViewModel ToReferenceViewModel(this Member m)
        {
            return new ReferenceViewModel()
            {
                Uid = m.Uid,
                Parent = m.Parent.Uid,
                IsExternal = false,
                Name = m.Name,
                NameWithType = ((Models.Type)m.Parent).Name + '.' + m.Name,
                FullName = m.FullName
            };
        }

        public static List<ReferenceViewModel> ToReferenceViewModels(this Member m, ECMAStore store)
        {
            var refs = new List<ReferenceViewModel>() {
                m.ToReferenceViewModel()
            };

            if (!string.IsNullOrEmpty(m.ReturnValueType))
            {
                var reference = GenerateReferenceByTypeString(m.ReturnValueType, store);
                if (reference != null)
                {
                    refs.Add(reference);
                }
            }

            if (m.Parameters?.Count > 0)
            {
                refs.AddRange(m.Parameters.Select(p => GenerateReferenceByTypeString(p.Type, store)).Where(r => r != null));
            }

            return refs;
        }

        private static ReferenceViewModel GenerateReferenceByTypeString(string type, ECMAStore store)
        {
            if (store.TypesByFullName.ContainsKey(type))
            {
                var rt = store.TypesByFullName[type];
                return new ReferenceViewModel()
                {
                    Uid = rt.Uid,
                    Parent = rt.Parent.Uid,
                    IsExternal = false,
                    Name = rt.Name,
                    NameWithType = rt.Name,
                    FullName = rt.FullName
                };
            }

            return null;
        }

        private static string ToSpecId(string typeStr)
        {
            return typeStr?.Replace('<', '{').Replace('>', '}');
        }
    }
}
