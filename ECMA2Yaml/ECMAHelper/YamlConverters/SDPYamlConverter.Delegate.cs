using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Linq;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        public DelegateSDPModel FormatDelegate(Type t)
        {
            var sdpDelegate = InitWithBasicProperties<DelegateSDPModel>(t);

            sdpDelegate.TypeParameters = ConvertTypeParameters(t);
            sdpDelegate.Inheritances = t.InheritanceChains?.LastOrDefault().Values.Select(uid => UidToTypeMDString(uid, _store)).ToList();

            if (t.ReturnValueType != null)
            {
                var returns = t.ReturnValueType;
                var r = returns.VersionedTypes
                    .Where(v => !string.IsNullOrWhiteSpace(v.Value) && v.Value != "System.Void");
                if (r.Any())
                {
                    foreach (var vt in returns.VersionedTypes)
                    {
                        vt.Value = SDPYamlConverter.TypeStringToTypeMDString(vt.Value, _store);
                    }
                    sdpDelegate.ReturnsWithMoniker = returns;
                }
            }

            sdpDelegate.Parameters = t.Parameters?.Select(p => ConvertNamedParameter(p, t.TypeParameters, t.Signatures.DevLangs))
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
