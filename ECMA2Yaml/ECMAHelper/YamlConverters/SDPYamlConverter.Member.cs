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
        public MemberSDPModel FormatSingleMember(Member m)
        {
            var sdpMember = InitWithBasicProperties<MemberSDPModel>(m);

            sdpMember.TypeParameters = ConvertTypeParameters(m);
            sdpMember.ThreadSafety = ConvertThreadSafety(m);
            sdpMember.Implements = m.Implements?.Select(commentId => DocIdToTypeMDString(commentId, _store))
                .Where(str => str != null)
                .ToList().NullIfEmpty();

            var knowTypeParams = m.Parent.TypeParameters.ConcatList(m.TypeParameters);

            if (m.ReturnValueType != null && m.ItemType != ItemType.Event)
            {
                var returns = m.ReturnValueType;


                if (returns.VersionedTypes.Count() == 1 )
                {
                    var oneReturn = returns.VersionedTypes.First();
                    if (oneReturn != null
                    && !string.IsNullOrEmpty(oneReturn.Value)
                    && oneReturn.Value != "System.Void")
                    {
                        sdpMember.Returns = new TypeReference()
                        {
                            Description = returns.Description,
                            Type = SDPYamlConverter.TypeStringToTypeMDString(oneReturn.Value, _store)
                        };
                    }
                }
                else
                {
                    var r = returns.VersionedTypes
                        .Where(v => !string.IsNullOrWhiteSpace(v.Value) && v.Value != "System.Void").ToArray();
                    if (r.Any())
                    {
                        foreach (var t in returns.VersionedTypes)
                        {
                            t.Value = SDPYamlConverter.TypeStringToTypeMDString(t.Value, _store);
                        }
                        var returnType = new ReturnValue
                        {
                            VersionedTypes = r,
                            Description = returns.Description
                        };
                        sdpMember.ReturnsWithMoniker = returnType;
                    }
                }
            }

            sdpMember.Parameters = m.Parameters?.Select(p => ConvertNamedParameter(p, knowTypeParams))
                .ToList().NullIfEmpty();

            sdpMember.Exceptions = m.Docs.Exceptions?.Select(
                p => new TypeReference()
                {
                    Description = p.Description,
                    Type = UidToTypeMDString(p.Uid, _store)
                }).ToList().NullIfEmpty();

            sdpMember.Permissions = m.Docs.Permissions?.Select(
                p => new TypeReference()
                {
                    Description = p.Description,
                    Type = DocIdToTypeMDString(p.CommentId, _store)
                }).ToList().NullIfEmpty();

            if (m.Attributes != null
                && m.Attributes.Any(attr => attr.Declaration == "System.CLSCompliant(false)"))
            {
                sdpMember.IsNotClsCompliant = true;
            }
            sdpMember.AltCompliant = m.Docs.AltCompliant.ResolveCommentId(_store)?.Uid;

            return sdpMember;
        }
    }
}
