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
        public DelegateSDPModel FormatDelegate(Type t)
        {
            var sdpDelegate = InitWithBasicProperties<DelegateSDPModel>(t);

            sdpDelegate.TypeParameters = ConvertTypeParameters(t);
            sdpDelegate.Inheritances = t.InheritanceChains?.LastOrDefault().Values.Select(uid => UidToTypeMDString(uid, _store)).ToList();

            if (t.ReturnValueType != null && t.ItemType != ItemType.Event)
            {
                var returns = t.ReturnValueType;

                if (returns.VersionedTypes.Count() == 1)
                {
                    var oneReturn = returns.VersionedTypes.First();
                    if (oneReturn != null
                    && !string.IsNullOrWhiteSpace(oneReturn.Value)
                    && oneReturn.Value != "System.Void")
                    {
                        sdpDelegate.Returns = new TypeReference()
                        {
                            Type = oneReturn.Value,
                            Description = returns.Description
                        };
                    }
                }
                else
                {
                    var r = returns.VersionedTypes
                        .Where(v => !string.IsNullOrWhiteSpace(v.Value) && v.Value != "System.Void");
                    if (r.Any())
                    {
                        sdpDelegate.ReturnsWithMoniker = returns;
                    }
                }
            }
            
            sdpDelegate.Parameters = t.Parameters?.Select(p => ConvertNamedParameter(p, t.TypeParameters))
                .ToList().NullIfEmpty();

            if (t.Attributes != null
                && t.Attributes.Any(attr => attr.Declaration == "System.CLSCompliant(false)"))
            {
                sdpDelegate.IsNotClsCompliant = true;
            }
            sdpDelegate.AltCompliant = t.Docs.AltCompliant.ResolveCommentId(_store)?.Uid;

            if (t.ExtensionMethods?.Count > 0)
            {
                sdpDelegate.ExtensionMethods = t.ExtensionMethods.Select(im => ExtensionMethodToTypeMemberLink(t, im))
                    .Where(ext => ext != null).ToList();
            }

            return sdpDelegate;
        }
    }
}
