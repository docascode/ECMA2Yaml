using ECMA2Yaml.Models;
using Microsoft.DocAsCode.DataContracts.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Monodoc.Ecma;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            if (ns.Frameworks != null)
            {
                item.Metadata.Add("version", ns.Frameworks);
            }
            if (!string.IsNullOrEmpty(ns.ECMASourcePath))
            {
                item.Metadata.Add("content_git_url", ns.ECMASourcePath);
            }
            
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
                var allExceptions = pv.Items.Where(i => i.Exceptions != null).SelectMany(i => i.Exceptions).ToArray();
                foreach (var ex in allExceptions)
                {
                    if (store.TypesByUid.ContainsKey(ex.Type))
                    {
                        pv.References.Add(store.TypesByUid[ex.Type].ToReferenceViewModel());
                    }
                    else if (store.MembersByUid.ContainsKey(ex.Type))
                    {
                        pv.References.Add(store.MembersByUid[ex.Type].ToReferenceViewModel());
                    }
                    else
                    {
                        OPSLogger.LogUserWarning("Referenced exception type not found: " + ex.Type, t.FullName);
                        pv.References.Add(new ReferenceViewModel()
                        {
                            Uid = ex.Type,
                            IsExternal = true,
                            Name = ex.Type
                        });
                    }
                }
                pv.References.AddRange(t.Members.SelectMany(m => m.ToReferenceViewModels(store)));
                if (t.Overloads?.Count > 0)
                {
                    pv.References.AddRange(t.Overloads.Select(o => o.ToReferenceViewModel()));
                }
            }
            pv.References = pv.References.DistinctBy(r => r.Uid).ToList();

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
                Type = t.ItemType.ToMemberType(),
                Children = t.Members?.Select(m => m.Uid).ToList(),
                Syntax = t.ToSyntaxDetailViewModel(store),
                Implements = t.Interfaces,
                Inheritance = t.InheritanceUids,
                InheritedMembers = t.InheritedMembers?.Select(p => p.Value + '.' + p.Key).OrderBy(s => s).ToList(),
                SupportedLanguages = languageList,
                Summary = t.Docs?.Summary,
                Remarks = t.Docs?.Remarks,
                Examples = string.IsNullOrEmpty(t.Docs?.Examples) ? null : new List<string> { t.Docs?.Examples }
            };
            if (t.Frameworks != null)
            {
                item.Metadata.Add("version", t.Frameworks);
            }
            if (!string.IsNullOrEmpty(t.ECMASourcePath))
            {
                item.Metadata.Add("content_git_url", t.ECMASourcePath);
            }
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
                Type = m.ItemType.ToMemberType(),
                AssemblyNameList = t.AssemblyInfo.Select(a => a.Name).ToList(),
                NamespaceName = t.Parent.Name,
                Overload = m.Overload,
                Syntax = m.ToSyntaxDetailViewModel(store),
                IsExplicitInterfaceImplementation = m.ItemType != ItemType.Constructor && m.Name.Contains('.'),
                SupportedLanguages = languageList,
                Summary = m.Docs?.Summary,
                Remarks = m.Docs?.Remarks,
                Examples = string.IsNullOrEmpty(m.Docs?.Examples) ? null : new List<string> { m.Docs?.Examples },
                Exceptions = m.Docs.Exceptions?.Select(ex => new ExceptionInfo() { CommentId = ex.CommentId, Description = ex.Description, Type = ex.Uid }).ToList()
            };
            if (m.Frameworks != null)
            {
                item.Metadata.Add("version", m.Frameworks);
            }
            if (!string.IsNullOrEmpty(m.ECMASourcePath))
            {
                item.Metadata.Add("content_git_url", m.ECMASourcePath);
            }
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
                    var refModel = new ReferenceViewModel()
                    {
                        Uid = desc.ToSpecId(),
                        Parent = string.IsNullOrEmpty(desc.Namespace) ? null : desc.Namespace,
                        IsExternal = store.TypesByUid.ContainsKey(desc.ToOuterTypeUid()) ? false : true,
                        Name = desc.ToDisplayName(),
                        NameWithType = desc.ToDisplayName(),
                        FullName = typeStr
                    };
                    if (desc.GenericTypeArgumentsCount > 0 || desc.ArrayDimensions?.Count > 0 || desc.DescModifier == Monodoc.Ecma.EcmaDesc.Mod.Pointer)
                    {
                        refModel.Specs.Add("csharp", desc.ToSpecItems());
                    }
                    return refModel;
                }
            }

            return null;
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

        public static MemberType ToMemberType(this ItemType itemType)
        {
            return (MemberType)Enum.Parse(typeof(MemberType), itemType.ToString());
        }
    }
}
