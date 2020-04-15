using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Collections.Generic;
using System.Linq;

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

            if (_withVersioning)
            {
                sdpOverload.AssembliesWithMoniker = sdpOverload.Members
                .Where(m => m.AssembliesWithMoniker != null)
                .SelectMany(m => m.AssembliesWithMoniker)
                .GroupBy(vs => vs.Value)
                .Select(g => new VersionedString()
                {
                    Value = g.Key,
                    Monikers = g.Any(v => v.Monikers == null) ? null : g.SelectMany(v => v.Monikers).ToHashSet()
                }).ToList().NullIfEmpty();

                sdpOverload.PackagesWithMoniker = sdpOverload.Members
                .Where(m => m.PackagesWithMoniker != null)
                .SelectMany(m => m.PackagesWithMoniker)
                .GroupBy(vp => vp.Value)
                .Select(g => new VersionedString()
                {
                    Value = g.Key,
                    Monikers = g.Any(v => v.Monikers == null) ? null : g.SelectMany(v => v.Monikers).ToHashSet()
                }).ToList().NullIfEmpty();
            }
            else
            {
                sdpOverload.Assemblies = sdpOverload.Members
                .Where(m => m.Assemblies != null)
                .SelectMany(m => m.Assemblies)
                .Distinct().ToList().NullIfEmpty();
            }

            sdpOverload.Namespace = sdpOverload.Members.First().Namespace;
            sdpOverload.DevLangs = sdpOverload.Members.SelectMany(m => m.DevLangs).Distinct().ToList();
            sdpOverload.Monikers = sdpOverload.Members.Where(m => m.Monikers != null).SelectMany(m => m.Monikers).Distinct().ToList();

            foreach (var m in sdpOverload.Members)
            {
                m.Namespace = null;
                m.Assemblies = null;
                m.AssembliesWithMoniker = null;
                m.PackagesWithMoniker = null;
                m.DevLangs = null;
            }

            GenerateRequiredMetadata(sdpOverload, overload ?? members.First(), members.Cast<ReflectionItem>().ToList());

            return sdpOverload;
        }
    }
}
