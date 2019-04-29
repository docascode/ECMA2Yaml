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
        public OverloadSDPModel FormatOverload(Member overload, List<Member> members)
        {
            var sdpOverload = new OverloadSDPModel()
            {
                Uid = overload?.Uid,
                CommentId = overload?.CommentId,
                Name = overload?.DisplayName,
                FullName = overload?.FullDisplayName,
                Summary = overload?.Docs?.Summary,
                Remarks = overload?.Docs?.Remarks,
                Examples = overload?.Docs?.Examples,
                Type = members.First().ItemType.ToString().ToLower(),
                Members = members.Select(m => FormatSingleMember(m)).ToList()
            };
            if (overload != null)
            {
                sdpOverload.NameWithType = members.First().Parent.Name + "." + sdpOverload.Name;
            }
            sdpOverload.Assemblies = sdpOverload.Members.SelectMany(m => m.Assemblies).Distinct().ToList();
            sdpOverload.Namespace = sdpOverload.Members.First().Namespace;
            sdpOverload.DevLangs = sdpOverload.Members.SelectMany(m => m.DevLangs).Distinct().ToList();

            foreach (var m in sdpOverload.Members)
            {
                m.Namespace = null;
                m.Assemblies = null;
                m.DevLangs = null;
            }

            GenerateRequiredMetadata(sdpOverload, overload ?? members.First());

            return sdpOverload;
        }
    }
}
