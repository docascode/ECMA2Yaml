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
        public static Dictionary<string, PageViewModel> GenerateNamespacePages(ECMAStore store)
        {
            var rval = new Dictionary<string, PageViewModel>();

            foreach (var ns in store.Namespaces)
            {
                rval.Add(ns.Key, ns.Value.ToPageViewModel());
            }

            return rval;
        }

        public static Dictionary<string, PageViewModel> GenerateTypePages(ECMAStore store)
        {
            var rval = new Dictionary<string, PageViewModel>();

            foreach (var t in store.TypesByUid)
            {
                rval.Add(t.Key, t.Value.ToPageViewModel(store));
            }

            return rval;
        }
    }

    public static class ModalConversionExtensions
    {
        private static string[] languageList = new string[] { "csharp" };
        private static List<string> platformList = new List<string>() { "net-11", "net-20", "netcore-10" };
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
                Type = MemberType.Namespace,
                SupportedLanguages = languageList,
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
            pv.Items.Add(t.ToItemViewModel(store));
            pv.References = new List<ReferenceViewModel>();
            if (t.BaseType != null)
            {
                pv.References.Add(t.BaseType.ToReferenceViewModel(store));
            }
            if (t.Members != null)
            {
                pv.Items.AddRange(t.Members.Select(m => m.ToItemViewModel(store)));
                pv.References.AddRange(t.Members.SelectMany(m => m.ToReferenceViewModels(store)));
                if (t.Overloads?.Count > 0)
                {
                    pv.References.AddRange(t.Overloads.Select(o => o.ToReferenceViewModel()));
                }
            }

            return pv;
        }

        public static ItemViewModel ToItemViewModel(this Models.Type t, ECMAStore store)
        {
            var item = new ItemViewModel()
            {
                Id = t.Id,
                Uid = t.Uid,
                Name = t.Name,
                NameWithType = t.Name,
                FullName = t.FullName,
                Type = t.MemberType,
                Children = t.Members?.Select(m => m.Uid).ToList(),
                Syntax = t.ToSyntaxDetailViewModel(store),
                Implements = t.Interfaces,
                Inheritance = t.InheritanceUids,
                InheritedMembers = t.InheritedMembers?.Select(p => p.Value + '.' + p.Key).OrderBy(s => s).ToList(),
                SupportedLanguages = languageList,
                Platform = platformList,
                Summary = t.Docs?.Summary,
                Remarks = t.Docs?.Remarks,
                Examples = string.IsNullOrEmpty(t.Docs?.Examples) ? null : new List<string> { t.Docs?.Examples }
            };
            return item;
        }

        public static SyntaxDetailViewModel ToSyntaxDetailViewModel(this Models.Type t, ECMAStore store)
        {
            var contentBuilder = new StringBuilder();
            if (t.Attributes?.Count > 0)
            {
                foreach (var att in t.Attributes)
                {
                    contentBuilder.AppendFormat("[{0}]\n", att);
                }
            }
            contentBuilder.Append(t.Signatures["C#"]);
            var content = contentBuilder.ToString();
            var syntax = new SyntaxDetailViewModel()
            {
                Content = content,
                //ContentForCSharp = content,
                TypeParameters = t.TypeParameters?.Select(tp => tp.ToApiParameter(store)).ToList()
            };

            return syntax;
        }

        public static ItemViewModel ToItemViewModel(this Member m, ECMAStore store)
        {
            var t = ((Models.Type)m.Parent);
            var item = new ItemViewModel()
            {
                Id = m.Id,
                Uid = m.Uid,
                Name = m.DisplayName,
                NameWithType = t.Name + '.' + m.DisplayName,
                FullName = m.FullDisplayName,
                Parent = m.Parent.Uid,
                Type = m.MemberType,
                AssemblyNameList = t.AssemblyInfo.Select(a => a.Name).ToList(),
                NamespaceName = t.Parent.Name,
                Overload = m.Overload,
                Syntax = m.ToSyntaxDetailViewModel(store),
                IsExplicitInterfaceImplementation = m.MemberType != MemberType.Constructor && m.Name.Contains('.'),
                SupportedLanguages = languageList,
                Platform = platformList,
                Summary = m.Docs?.Summary,
                Remarks = m.Docs?.Remarks,
                Examples = string.IsNullOrEmpty(m.Docs?.Examples) ? null : new List<string> { m.Docs?.Examples },
                Exceptions = m.Docs.Exceptions?.Select(ex => new ExceptionInfo() { CommentId = ex.CommentId, Description = ex.Description, Type = ex.Uid }).ToList()
            };
            return item;
        }

        public static SyntaxDetailViewModel ToSyntaxDetailViewModel(this Member m, ECMAStore store)
        {
            var syntax = new SyntaxDetailViewModel()
            {
                Content = m.Signatures["C#"],
                Parameters = m.Parameters?.Select(p => p.ToApiParameter(store)).ToList()
            };
            if (m.ReturnValueType != null && !string.IsNullOrEmpty(m.ReturnValueType.Type) && m.ReturnValueType.Type != "System.Void")
            {
                syntax.Return = m.ReturnValueType.ToApiParameter(store);
            }
            return syntax;
        }

        public static ApiParameter ToApiParameter(this Parameter p, ECMAStore store)
        {
            string str = null;
            if (!string.IsNullOrEmpty(p.Type))
            {
                str = store.TypesByFullName.ContainsKey(p.Type) ? store.TypesByFullName[p.Type].Uid : p.Type.ToSpecId();
            }
            var ap = new ApiParameter()
            {
                Name = p.Name,
                Type = str,
                Description = p.Description ?? "To be added."
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
                Name = m.DisplayName,
                NameWithType = ((Models.Type)m.Parent).Name + '.' + m.DisplayName,
                FullName = m.FullDisplayName
            };
        }

        public static List<ReferenceViewModel> ToReferenceViewModels(this Member m, ECMAStore store)
        {
            var refs = new List<ReferenceViewModel>() {
                m.ToReferenceViewModel()
            };

            if (m.ReturnValueType != null && !string.IsNullOrEmpty(m.ReturnValueType.Type) && m.ReturnValueType.Type != "System.Void")
            {
                var reference = GenerateReferenceByTypeString(m.ReturnValueType.Type, store);
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

        public static ReferenceViewModel ToReferenceViewModel(this BaseType bt, ECMAStore store)
        {
            return new ReferenceViewModel()
            {
                Uid = bt.Uid,
                IsExternal = store.MembersByUid.ContainsKey(bt.Uid),
                Name = bt.Name,
            };
        }

        private static ReferenceViewModel GenerateReferenceByTypeString(string typeStr, ECMAStore store)
        {
            if (store.TypesByFullName.ContainsKey(typeStr))
            {
                var rt = store.TypesByFullName[typeStr];
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
            else
            {
                var desc = ECMAStore.GetOrAddTypeDescriptor(typeStr);
                if (desc != null)
                {
                    return new ReferenceViewModel()
                    {
                        Uid = desc.ToSpecId(),
                        Parent = desc.Namespace,
                        IsExternal = store.TypesByUid.ContainsKey(desc.ToOuterTypeUid()) ? false : true,
                        Name = desc.ToDisplayName(),
                        NameWithType = desc.ToDisplayName(),
                        FullName = typeStr
                    };
                }
            }

            return null;
        }
    }
}
