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

            foreach (var ns in store.Namespaces.Where(ns => !string.IsNullOrEmpty(ns.Key)))
            {
                rval.Add(ns.Key, ns.Value.ToPageViewModel(store));
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
        public static PageViewModel ToPageViewModel(this Namespace ns, ECMAStore store)
        {
            var item = new ItemViewModel()
            {
                Id = ns.Id,
                Uid = ns.Uid,
                CommentId = ns.CommentId,
                Name = ns.Name,
                NameWithType = ns.Name,
                FullName = ns.Name,
                Type = MemberType.Namespace,
                SupportedLanguages = languageList,
                Children = ns.Types.Select(t => t.Uid).ToList(),
                Summary = ns.Docs?.Summary,
                Remarks = ns.Docs?.Remarks,
                Examples = string.IsNullOrEmpty(ns.Docs?.Examples) ? null : new List<string> { ns.Docs?.Examples },
                SeeAlsos = ns.Docs.BuildSeeAlsoList(store),
                Source = ns.SourceDetail.ToSourceDetail()
            };
            item.Metadata[OPSMetadata.Monikers] = ns.Monikers;
            item.Metadata.MergeMetadata(ns.Metadata);
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

        public static ReferenceViewModel ToReferenceViewModel(this Models.Type t)
        {
            var rval = new ReferenceViewModel()
            {
                Uid = t.Uid,
                Parent = t.Parent.Uid,
                IsExternal = false,
                Name = t.Name,
                NameWithType = t.FullName,
                FullName = t.FullName,
                CommentId = t.CommentId
            };
            if (t.ItemType != ItemType.Default)
            {
                rval.Additional["type"] = t.ItemType.ToString().ToLower();
            }
            return rval;
        }

        public static ReferenceViewModel ToReferenceViewModel(this Namespace n)
        {
            var rval = new ReferenceViewModel()
            {
                Uid = n.Uid,
                IsExternal = false,
                Name = n.Name,
                NameWithType = n.Name,
                FullName = n.Name,
                CommentId = n.CommentId
            };
            rval.Additional["type"] = ItemType.Namespace.ToString().ToLower();
            return rval;
        }

        public static PageViewModel ToPageViewModel(this Models.Type t, ECMAStore store)
        {
            var pv = new PageViewModel();
            var tItem = t.ToItemViewModel(store);
            pv.Items = new List<ItemViewModel>();
            pv.Items.Add(tItem);
            pv.Metadata = t.ExtendedMetadata;
            pv.References = new List<ReferenceViewModel>();
            if (!string.IsNullOrEmpty(t.Parent?.Uid))
            {
                pv.References.Add((t.Parent as Namespace).ToReferenceViewModel());
            }
            if (t.BaseTypes != null)
            {
                pv.References.AddRange(t.BaseTypes.SelectMany(bt => bt.ToReferenceViewModel(store)));
            }
            pv.References.AddRange(t.GenerateReferencesFromParameters(store));
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
                pv.References.AddRange(t.InheritedMembers.Keys.Select(ex => store.MembersByUid[ex].ToReferenceViewModel()));
            }
            if (t.ExtensionMethods?.Count > 0)
            {
                pv.References.AddRange(t.ExtensionMethods.Select(ex => store.MembersByUid[ex.Value].ToReferenceViewModel()));
            }
            if (t.Interfaces?.Count > 0)
            {
                pv.References.AddRange(t.Interfaces.SelectMany(i => GenerateReferencesByTypeString(i, store)));
            }
            if (tItem.DerivedClasses?.Count > 0)
            {
                pv.References.AddRange(tItem.DerivedClasses.Select(c => store.TypesByUid[c].ToReferenceViewModel()));
            }
            if (t.ReturnValueType != null
                && !string.IsNullOrEmpty(t.ReturnValueType.Type)
                && t.ReturnValueType.Type != "System.Void")
            {
                pv.References.AddRange(GenerateReferencesByTypeString(t.ReturnValueType.Type, store));
            }
            pv.References = pv.References.DistinctBy(r => r.Uid).ToList();

            return pv;
        }

        public static ItemViewModel ToItemViewModel(this Models.Type t, ECMAStore store)
        {
            var syntax = t.ToSyntaxDetailViewModel(store);
            var item = new ItemViewModel()
            {
                Id = t.Id,
                Uid = t.Uid,
                CommentId = t.CommentId,
                Name = t.Name,
                NameWithType = t.FullName,
                FullName = t.FullName,
                Type = t.ItemType.ToMemberType(),
                NamespaceName = t.Parent.Name,
                Children = t.Members?.Select(m => m.Uid).ToList(),
                Syntax = syntax,
                Implements = t.Interfaces?.Where(i => i != null).Select(i => store.TypesByFullName.ContainsKey(i) ? store.TypesByFullName[i].Uid : i.ToSpecId()).ToList(),
                Inheritance = t.InheritanceChains?.LastOrDefault()?.Values,
                AssemblyNameList = store.UWPMode ? null : t.AssemblyInfo.Select(a => a.Name).Distinct().ToList(),
                InheritedMembers = t.InheritedMembers?.Select(p => p.Value.Value).OrderBy(s => s).ToList(),
                SupportedLanguages = syntax.Contents?.Keys?.ToArray(),
                Summary = t.Docs?.Summary,
                Remarks = t.Docs?.Remarks,
                Examples = string.IsNullOrEmpty(t.Docs?.Examples) ? null : new List<string> { t.Docs?.Examples },
                ExtensionMethods = t.ExtensionMethods?.Select(ext => ext.Value).ToList(),
                Attributes = t.Attributes.GetAttributeInfo(store),
                Modifiers = t.Modifiers,
                SeeAlsos = t.Docs.BuildSeeAlsoList(store),
                Source = t.SourceDetail.ToSourceDetail()
            };
            item.Metadata[OPSMetadata.Monikers] = t.Monikers;
            item.Metadata.MergeMetadata(t.Metadata);
            item.Metadata.AddPermissions(t.Docs);
            //item.Metadata.AddThreadSafety(t.Docs);

            //not top level class like System.Object, has children
            if (t.ItemType == ItemType.Interface
                && store.ImplementationChildrenByUid.ContainsKey(t.Uid))
            {
                item.DerivedClasses = store.ImplementationChildrenByUid[t.Uid].Select(v => v.Value).ToList();
            }
            else if (store.InheritanceParentsByUid.ContainsKey(t.Uid) 
                && store.InheritanceParentsByUid[t.Uid]?.Count > 0
                && store.InheritanceChildrenByUid.ContainsKey(t.Uid))
            {
                item.DerivedClasses = store.InheritanceChildrenByUid[t.Uid].Select(v => v.Value).ToList();
            }
            return item;
        }

        public static SyntaxDetailViewModel ToSyntaxDetailViewModel(this ReflectionItem item, ECMAStore store)
        {
            const string csharp = "C#";
            var contents = store.UWPMode ? ConverterHelper.BuildUWPSignatures(item) : ConverterHelper.BuildSignatures(item);

            var syntax = new SyntaxDetailViewModel()
            {
                Contents = contents,
                Content = contents.ContainsKey(Models.ECMADevLangs.OPSMapping[csharp]) ? contents[Models.ECMADevLangs.OPSMapping[csharp]] : null,
                Parameters = item.Parameters?.Select(p => p.ToApiParameter(store))?.ToList(),
                TypeParameters = item.TypeParameters?.Select(tp => tp.ToApiParameter(store))?.ToList()
            };
            if (item.ReturnValueType != null
                && !string.IsNullOrEmpty(item.ReturnValueType.Type)
                && item.ReturnValueType.Type != "System.Void"
                && item.ItemType != ItemType.Event)
            {
                syntax.Return = item.ReturnValueType.ToApiParameter(store);
            }
            return syntax;
        }

        public static ItemViewModel ToItemViewModel(this Member m, ECMAStore store)
        {
            var t = ((Models.Type)m.Parent);
            var syntax = m.ToSyntaxDetailViewModel(store);
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
                AssemblyNameList = store.UWPMode ? null : m.AssemblyInfo.Select(a => a.Name).Distinct().ToList(),
                NamespaceName = t.Parent.Name,
                Overload = m.Overload,
                Syntax = syntax,
                IsExplicitInterfaceImplementation = m.IsEII,
                IsExtensionMethod = m.IsExtensionMethod,
                SupportedLanguages = syntax.Contents?.Keys?.ToArray(),
                Summary = m.Docs?.Summary,
                Remarks = m.Docs?.Remarks,
                Examples = string.IsNullOrEmpty(m.Docs?.Examples) ? null : new List<string> { m.Docs?.Examples },
                Exceptions = m.Docs.Exceptions?.Select(ex => new ExceptionInfo() { CommentId = ex.CommentId, Description = ex.Description, Type = ex.Uid }).ToList(),
                Attributes = m.Attributes.GetAttributeInfo(store),
                Modifiers = m.Modifiers,
                SeeAlsos = m.Docs.BuildSeeAlsoList(store),
                Source = m.SourceDetail.ToSourceDetail()
            };
            var implements = m.Implements?.Select(commentId => commentId.ResolveCommentId(store)?.Uid)?.Where(uid => uid != null)?.ToList();
            if (implements?.Count > 0)
            {
                item.Implements = implements;
            }
            item.Metadata[OPSMetadata.Monikers] = m.Monikers;
            item.Metadata.MergeMetadata(m.Metadata);
            item.Metadata.AddPermissions(m.Docs);
            //item.Metadata.AddThreadSafety(m.Docs);
            return item;
        }

        public static ApiParameter ToApiParameter(this Parameter p, ECMAStore store)
        {
            string str = null;
            if (!string.IsNullOrEmpty(p.Type))
            {
                str = store.TypesByFullName.ContainsKey(p.Type) ? store.TypesByFullName[p.Type].Uid : (p.OriginalTypeString ?? p.Type).ToSpecId();
            }
            var ap = new ApiParameter()
            {
                Name = p.Name,
                Type = str,
                Description = p.Description ?? ""
            };

            return ap;
        }

        public static ApiParameter ToApiParameter(this TypeParameter tp, ECMAStore store)
        {
            return new ApiParameter()
            {
                Name = tp.Name,
                Description = tp.Description ?? ""
            };
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
                r.Additional[OPSMetadata.Monikers] = m.Monikers;
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
                var reference = GenerateReferencesByTypeString(m.ReturnValueType.Type, store, m.ReturnValueType.OriginalTypeString);
                if (reference != null)
                {
                    refs.AddRange(reference);
                }
            }

            refs.AddRange(m.GenerateReferencesFromParameters(store));

            return refs;
        }

        public static List<ReferenceViewModel> GenerateReferencesFromParameters(this ReflectionItem item, ECMAStore store)
        {
            var refs = new List<ReferenceViewModel>();

            if (item.Parameters?.Count > 0)
            {
                refs.AddRange(item.Parameters.SelectMany(p => GenerateReferencesByTypeString(p.Type, store)).Where(r => r != null));
            }

            var typeParameters = item.TypeParameters ?? new List<TypeParameter>();
            if (item.Parent != null && item.Parent is Models.Type)
            {
                var t = item.Parent as Models.Type;
                if (t?.TypeParameters != null)
                {
                    typeParameters.AddRange(t.TypeParameters);
                }
            }
            refs = refs.Where(r => !typeParameters.Any(tp => tp.Name == r.Uid)).ToList();

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
                IsExternal = true,
                NameWithType = spec.NameWithType,
                FullName = spec.FullName
            };
        }

        private static List<ReferenceViewModel> GenerateReferencesByTypeString(string typeStr, ECMAStore store, string originalTypeStr = null)
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
                var desc = ECMAStore.GetOrAddTypeDescriptor(originalTypeStr ?? typeStr);
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
                    if (desc.GenericTypeArgumentsCount > 0 || desc.ArrayDimensions?.Count > 0 || desc.DescModifier == Monodoc.Ecma.EcmaDesc.Mod.Pointer || desc.NestedType != null)
                    {
                        refModel.IsExternal = null;
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
                        OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_TypeString_ParseFailed, null, typeStr);
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

        public static List<SpecViewModel> ToSpecItems(this EcmaDesc desc, string parentTypeUid = null)
        {
            var uid = string.IsNullOrEmpty(parentTypeUid) ? desc.ToOuterTypeUid() : (parentTypeUid + "." + desc.ToOuterTypeUid());
            List <SpecViewModel> list = new List<SpecViewModel>();
            list.Add(new SpecViewModel()
            {
                Name = desc.TypeName,
                NameWithType = desc.TypeName,
                FullName = desc.ToSpecItemFullName(),
                Uid = uid
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

            if (desc.NestedType != null)
            {
                list.Add(new SpecViewModel()
                {
                    Name = ".",
                    NameWithType = ".",
                    FullName = "."
                });
                list.AddRange(desc.NestedType.ToSpecItems(uid));
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

        public static void AddThreadSafety(this Dictionary<string, object> mta, Docs docs)
        {
            if (docs.ThreadSafetyInfo != null)
            {
                mta[OPSMetadata.ThreadSafetyInfo] = docs.ThreadSafetyInfo;
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

        public static List<LinkInfo> BuildSeeAlsoList(this Docs docs, ECMAStore store)
        {
            var altMembers = docs.AltMemberCommentIds?.Select(a => a.ResolveCommentId(store)?.ToLinkInfo()).Where(r => r != null).NullIfEmpty()?.ToList();
            var related = docs.Related?.Select(r => r.ToLinkInfo())?.ToList();
            return altMembers.MergeWith(related);
        }

        public static LinkInfo ToLinkInfo(this ReflectionItem item)
        {
            if (item == null)
            {
                return null;
            }
            var name = item.Name;
            if (item is Member m)
            {
                name = m.DisplayName;
            }
            else if (item is Models.Type t)
            {
                name = t.FullName;
            }
            return new LinkInfo()
            {
                LinkType = LinkType.CRef,
                LinkId = item.Uid,
                CommentId = item.CommentId,
                AltText = name
            };
        }

        public static LinkInfo ToLinkInfo(this RelatedTag tag)
        {
            if (tag == null)
            {
                return null;
            }
            return new LinkInfo()
            {
                LinkType = LinkType.HRef, 
                LinkId = tag.Uri,
                AltText = tag.Text
            };
        }

        public static SourceDetail ToSourceDetail(this GitSourceDetail source)
        {
            if (source == null)
            {
                return null;
            }
            return new SourceDetail()
            {
                Path = source.Path,
                Remote = new Microsoft.DocAsCode.Common.Git.GitDetail()
                {
                    RelativePath = source.Path,
                    RemoteBranch = source.RepoBranch,
                    RemoteRepositoryUrl = source.RepoUrl
                }
            };
        }
    }
}
