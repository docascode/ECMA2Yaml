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

    public static class ModelConversionExtensions
    {
        private static string[] languageList = new string[] { "csharp" };
        public static PageViewModel ToPageViewModel(this Namespace ns)
        {
            var item = ns.ToItemViewModel();
            var pv = new PageViewModel()
            {
                Items = new List<ItemViewModel>()
                {
                    item
                },
                References = ns.Types.Select(t => t.ToReferenceViewModel()).ToList()
            };
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
                Children = ns.Types.Select(t => t.Uid).ToList(),
                Summary = ns.Docs?.Summary,
                Remarks = ns.Docs?.Remarks,
                Examples = string.IsNullOrEmpty(ns.Docs?.Examples) ? null : new List<string> { ns.Docs?.Examples }
            };
            item.Metadata.MergeMetadata(ns.Metadata);

            return item;
        }

        public static ReferenceViewModel ToReferenceViewModel(this Models.Type t)
        {
            var rval = new ReferenceViewModel()
            {
                Uid = t.Uid,
                Parent = t.Parent.Uid,
                IsExternal = false,
                Name = t.Name,
                NameWithType = t.Name,
                FullName = t.FullName
            };
            if (t.ItemType != ItemType.Default)
            {
                rval.Additional["type"] = t.ItemType.ToString().ToLower();
            }
            return rval;
        }

        public static PageViewModel ToPageViewModel(this Models.Type t, ECMAStore store)
        {
            var pv = new PageViewModel();
            pv.Items = new List<ItemViewModel>();
            pv.Items.Add(t.ToItemViewModel(store));
            pv.References = new List<ReferenceViewModel>();
            if (t.BaseType != null)
            {
                pv.References.AddRange(t.BaseType.ToReferenceViewModel(store));
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
                    pv.References.AddRange(t.Overloads.Select(o =>
                    {
                        var r = o.ToReferenceViewModel(withMetadata: true);
                        r.CommentId = o.CommentId;
                        return r;
                    }
                    ));
                }
            }
            if (t.InheritedMembers?.Count > 0)
            {
                pv.References.AddRange(t.InheritedMembers.Select(p => p.Value + '.' + p.Key).Select(ex => store.MembersByUid[ex].ToReferenceViewModel()));
            }
            if (t.ExtensionMethods?.Count > 0)
            {
                pv.References.AddRange(t.ExtensionMethods.Select(ex => store.MembersByUid[ex].ToReferenceViewModel()));
            }
            if (t.Interfaces?.Count > 0)
            {
                pv.References.AddRange(t.Interfaces.SelectMany(i => GenerateReferencesByTypeString(i, store)));
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
                CommentId = t.CommentId,
                Name = t.Name,
                NameWithType = t.Name,
                FullName = t.FullName,
                Type = t.ItemType.ToMemberType(),
                NamespaceName = t.Parent.Name,
                Children = t.Members?.Select(m => m.Uid).ToList(),
                Syntax = t.ToSyntaxDetailViewModel(store),
                Implements = t.Interfaces?.Where(i => i != null).Select(i => store.TypesByFullName.ContainsKey(i) ? store.TypesByFullName[i].Uid : i.ToSpecId()).ToList(),
                Inheritance = t.InheritanceUids,
                AssemblyNameList = t.AssemblyInfo.Select(a => a.Name).ToList(),
                InheritedMembers = t.InheritedMembers?.Select(p => p.Value + '.' + p.Key).OrderBy(s => s).ToList(),
                SupportedLanguages = languageList,
                Summary = t.Docs?.Summary,
                Remarks = t.Docs?.Remarks,
                Examples = string.IsNullOrEmpty(t.Docs?.Examples) ? null : new List<string> { t.Docs?.Examples },
                ExtensionMethods = t.ExtensionMethods,
                Attributes = t.Attributes.GetAttributeInfo(store),
                Modifiers = t.Modifiers
            };
            item.Metadata.MergeMetadata(t.Metadata);
            item.Metadata.AddPermissions(t.Docs);
            //not top level class like System.Object, has children
            if ((t.ItemType == ItemType.Interface
                || (store.InheritanceParentsByUid.ContainsKey(t.Uid) && store.InheritanceParentsByUid[t.Uid]?.Count > 0))
                && store.InheritanceChildrenByUid.ContainsKey(t.Uid))
            {
                item.DerivedClasses = store.InheritanceChildrenByUid[t.Uid];
            }
            return item;
        }

        public static SyntaxDetailViewModel ToSyntaxDetailViewModel(this Models.Type t, ECMAStore store)
        {
            var contentBuilder = new StringBuilder();
            if (t.Attributes?.Count > 0)
            {
                foreach (var att in t.Attributes.Where(attr => attr.Visible))
                {
                    contentBuilder.AppendFormat("[{0}]\n", att.Declaration);
                }
            }
            contentBuilder.Append(t.Signatures["C#"]);
            var content = contentBuilder.ToString();
            var syntax = new SyntaxDetailViewModel()
            {
                Content = content,
                Parameters = t.Parameters?.Select(p => p.ToApiParameter(store))?.ToList(),
                TypeParameters = t.TypeParameters?.Select(tp => tp.ToApiParameter(store)).ToList()
            };
            if (t.ReturnValueType != null && !string.IsNullOrEmpty(t.ReturnValueType.Type) && t.ReturnValueType.Type != "System.Void")
            {
                syntax.Return = t.ReturnValueType.ToApiParameter(store);
            }
            return syntax;
        }

        public static ItemViewModel ToItemViewModel(this Member m, ECMAStore store)
        {
            var t = ((Models.Type)m.Parent);
            var item = new ItemViewModel()
            {
                Id = m.Id,
                Uid = m.Uid,
                CommentId = m.CommentId,
                Name = m.DisplayName,
                NameWithType = t.Name + '.' + m.DisplayName,
                FullName = m.FullDisplayName,
                Parent = m.Parent.Uid,
                Type = m.ItemType.ToMemberType(),
                AssemblyNameList = m.AssemblyInfo.Select(a => a.Name).ToList(),
                NamespaceName = t.Parent.Name,
                Overload = m.Overload,
                Syntax = m.ToSyntaxDetailViewModel(store),
                IsExplicitInterfaceImplementation = m.IsEII,
                SupportedLanguages = languageList,
                Summary = m.Docs?.Summary,
                Remarks = m.Docs?.Remarks,
                Examples = string.IsNullOrEmpty(m.Docs?.Examples) ? null : new List<string> { m.Docs?.Examples },
                Exceptions = m.Docs.Exceptions?.Select(ex => new ExceptionInfo() { CommentId = ex.CommentId, Description = ex.Description, Type = ex.Uid }).ToList(),
                Attributes = m.Attributes.GetAttributeInfo(store),
                Modifiers = m.Modifiers
            };
            item.Metadata.MergeMetadata(m.Metadata);
            item.Metadata.AddPermissions(m.Docs);
            return item;
        }

        public static SyntaxDetailViewModel ToSyntaxDetailViewModel(this Member m, ECMAStore store)
        {
            var contentBuilder = new StringBuilder();
            if (m.Attributes?.Count > 0)
            {
                foreach (var att in m.Attributes.Where(attr => attr.Visible))
                {
                    contentBuilder.AppendFormat("[{0}]\n", att.Declaration);
                }
            }
            contentBuilder.Append(m.Signatures["C#"]);
            var content = contentBuilder.ToString();
            var syntax = new SyntaxDetailViewModel()
            {
                Content = content,
                Parameters = m.Parameters?.Select(p => p.ToApiParameter(store))?.ToList(),
                TypeParameters = m.TypeParameters?.Select(p => p.ToApiParameter(store))?.ToList()
            };
            var returnType = m.ReturnValueType?.Type;
            if (!string.IsNullOrEmpty(returnType) && returnType != "System.Void" && m.ItemType != ItemType.Event)
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
                Description = p.Description ?? ""
            };

            return ap;
        }

        public static ReferenceViewModel ToReferenceViewModel(this Member m, bool withMetadata = false)
        {
            var r = new ReferenceViewModel()
            {
                Uid = m.Uid,
                Parent = m.Parent.Uid,
                IsExternal = false,
                Name = m.DisplayName,
                NameWithType = ((Models.Type)m.Parent).Name + '.' + m.DisplayName,
                FullName = m.FullDisplayName
            };
            if (withMetadata)
            {
                r.Additional.MergeMetadata(m.Metadata);
            }
            if (m.ItemType != ItemType.Default)
            {
                r.Additional["type"] = m.ItemType.ToString().ToLower();
            }
            return r;
        }

        public static List<ReferenceViewModel> ToReferenceViewModels(this Member m, ECMAStore store)
        {
            var refs = new List<ReferenceViewModel>() {
                m.ToReferenceViewModel()
            };

            if (m.ReturnValueType != null && !string.IsNullOrEmpty(m.ReturnValueType.Type) && m.ReturnValueType.Type != "System.Void")
            {
                var reference = GenerateReferencesByTypeString(m.ReturnValueType.Type, store);
                if (reference != null)
                {
                    refs.AddRange(reference);
                }
            }

            if (m.Parameters?.Count > 0)
            {
                refs.AddRange(m.Parameters.SelectMany(p => GenerateReferencesByTypeString(p.Type, store)).Where(r => r != null));
            }

            return refs;
        }

        public static List<ReferenceViewModel> ToReferenceViewModel(this BaseType bt, ECMAStore store)
        {
            return GenerateReferencesByTypeString(bt.Name, store);
        }

        public static ReferenceViewModel ToReferenceViewModel(this SpecViewModel spec, ECMAStore store)
        {
            if (store.TypesByUid.ContainsKey(spec.Uid))
            {
                return new ReferenceViewModel()
                {
                    Uid = store.TypesByUid[spec.Uid].Uid,
                    Name = store.TypesByUid[spec.Uid].Name,
                    NameWithType = store.TypesByUid[spec.Uid].Name,
                    FullName = store.TypesByUid[spec.Uid].FullName
                };
            }
            return new ReferenceViewModel()
            {
                Uid = spec.Uid,
                Name = spec.Name,
                NameWithType = spec.NameWithType,
                FullName = spec.FullName
            };
        }

        private static List<ReferenceViewModel> GenerateReferencesByTypeString(string typeStr, ECMAStore store)
        {
            if (store.TypesByFullName.ContainsKey(typeStr))
            {
                var rt = store.TypesByFullName[typeStr];
                return new List<ReferenceViewModel>() {new ReferenceViewModel()
                {
                    Uid = rt.Uid,
                    Parent = rt.Parent.Uid,
                    IsExternal = false,
                    Name = rt.Name,
                    NameWithType = rt.Name,
                    FullName = rt.FullName
                } };
            }
            else
            {
                var refs = new List<ReferenceViewModel>();
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
                        foreach (var spec in refModel.Specs["csharp"].Where(s => !string.IsNullOrEmpty(s.Uid)))
                        {
                            refs.Add(spec.ToReferenceViewModel(store));
                        }
                    }
                    refs.Add(refModel);
                }
                else
                {
                    if (typeStr.EndsWith("@") || typeStr.Contains("<?>") || typeStr.Contains(" modreq") || typeStr.Contains(" modopt"))
                    {
                        OPSLogger.LogUserInfo("Unable to parse type string " + typeStr);
                    }
                    else
                    {
                        OPSLogger.LogUserWarning("Unable to parse type string " + typeStr);
                    }
                    
                }
                return refs;
            }
        }

        public static string ToSpecItemFullName(this EcmaDesc desc)
        {
            if (string.IsNullOrEmpty(desc.Namespace))
            {
                return desc.TypeName;
            }
            else
            {
                return desc.Namespace + '.' + desc.TypeName;
            }
        }

        public static List<SpecViewModel> ToSpecItems(this EcmaDesc desc)
        {
            List<SpecViewModel> list = new List<SpecViewModel>();
            list.Add(new SpecViewModel()
            {
                Name = desc.TypeName,
                NameWithType = desc.TypeName,
                FullName = desc.ToSpecItemFullName(),
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

        public static void AddPermissions(this Dictionary<string, object> mta, Docs docs)
        {
            if (docs.Permissions?.Count > 0)
            {
                mta[OPSMetadata.Permissions] = docs.Permissions.Select(ex => new ExceptionInfo() { CommentId = ex.CommentId, Description = ex.Description, Type = ex.Uid }).ToList();
            }
        }

        public static void MergeMetadata(this Dictionary<string, object> mta, Dictionary<string, object> mta1)
        {
            if (mta != null && mta1 != null && mta1.Count > 0)
            {
                foreach (var pair in mta1)
                {
                    mta.Add(pair.Key, pair.Value);
                }
            }
        }

        public static List<AttributeInfo> GetAttributeInfo(this List<ECMAAttribute> attributes, ECMAStore store)
        {
            if (attributes == null)
            {
                return null;
            }
            return attributes.Where(attr => attr.Visible).Select(attr =>
            {
                return new AttributeInfo()
                {
                    Type = attr.TypeFullName
                };
            }).ToList();
        }
    }
}
