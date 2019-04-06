using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        public TypeSDPModel FormatType(Type t)
        {
            var sdpType = InitWithBasicProperties<TypeSDPModel>(t);

            sdpType.Type = t.ItemType.ToString().ToLower();
            sdpType.TypeParameters = ConvertTypeParameters(t);
            sdpType.ThreadSafety = ConvertThreadSafety(t);

            sdpType.Inheritances = t.InheritanceUids?.Select(uid => UidToTypeMDString(uid, _store))
                .ToList()
                .NullIfEmpty();
            sdpType.Implements = t.Interfaces?.Where(i => i != null)
                .Select(i => TypeStringToTypeMDString(i, _store))
                .ToList()
                .NullIfEmpty();
            sdpType.Permissions = t.Docs.Permissions?.Select(
                p => new TypeReference()
                {
                    Description = p.Description,
                    Type = DocIdToTypeMDString(p.CommentId, _store)
                })
                .ToList()
                .NullIfEmpty();

            //not top level class like System.Object, has children
            if ((t.ItemType == ItemType.Interface
                || (_store.InheritanceParentsByUid.ContainsKey(t.Uid) && _store.InheritanceParentsByUid[t.Uid]?.Count > 0))
                && _store.InheritanceChildrenByUid.ContainsKey(t.Uid))
            {
                sdpType.DerivedClasses = _store.InheritanceChildrenByUid[t.Uid];
            }

            PopulateTypeChildren(t, sdpType);

            return sdpType;
        }

        private void PopulateTypeChildren(Type t, TypeSDPModel sdpType)
        {
            var members = new List<Member>();
            if (t.Members != null)
            {
                members.AddRange(t.Members);
            }
            if (t.InheritedMembers != null)
            {
                members.AddRange(t.InheritedMembers.Select(p => p.Value + '.' + p.Key).Select(im => _store.MembersByUid[im]));
            }
            if (members.Count > 0)
            {
                var eiis = members.Where(m => m.IsEII).ToList();
                if (eiis.Count > 0)
                {
                    sdpType.EIIs = eiis.Select(m => ConvertMemberReference(m)).ToList();
                }
                foreach (var mGroup in members
                    .Where(m => !m.IsEII)
                    .GroupBy(m => m.ItemType))
                {
                    var list = mGroup.Select(m => ConvertMemberReference(m)).ToList();
                    switch (mGroup.Key)
                    {
                        case ItemType.Property:
                            sdpType.Properties = list;
                            break;
                        case ItemType.Method:
                            sdpType.Methods = list;
                            break;
                        case ItemType.Event:
                            sdpType.Events = list;
                            break;
                        case ItemType.Field:
                            sdpType.Fields = list;
                            break;
                        case ItemType.AttachedEvent:
                            sdpType.AttachedEvents = list;
                            break;
                        case ItemType.AttachedProperty:
                            sdpType.AttachedProperties = list;
                            break;
                        case ItemType.Constructor:
                            sdpType.Constructors = list;
                            break;
                        case ItemType.Operator:
                            sdpType.Operators = list;
                            break;
                    }
                }
            }
            if (t.ExtensionMethods?.Count > 0)
            {
                sdpType.ExtensionMethods = t.ExtensionMethods.Select(im => ConvertMemberReference(_store.MembersByUid[im])).ToList();
            }

            MemberReference ConvertMemberReference(Member m)
            {
                if (m == null)
                {
                    return null;
                }
                return new MemberReference()
                {
                    Uid = m.Uid,
                    InheritedFrom = m.Parent.Uid != t.Uid ? m.Parent.Uid : null
                };
            }
        }
    }
}
