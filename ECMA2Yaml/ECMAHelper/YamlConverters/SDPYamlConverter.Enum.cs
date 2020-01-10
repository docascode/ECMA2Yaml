using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Type = ECMA2Yaml.Models.Type;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        public EnumSDPModel FormatEnum(Type enumTypeItem, HashSet<string> memberTouchCache)
        {
            var sdpEnum = InitWithBasicProperties<EnumSDPModel>(enumTypeItem);

            if (_withVersioning)
            {
                sdpEnum.InheritancesWithMoniker = ConverterHelper.TrimMonikers(
                    enumTypeItem.InheritanceChains?.Select(
                    chain => new VersionedCollection<string>(
                        chain.Monikers,
                        chain.Values.Select(uid => UidToTypeMDString(uid, _store)).ToList()
                        )).ToList(),
                    enumTypeItem.Monikers);
            }
            else
            {
                sdpEnum.Inheritances = enumTypeItem.InheritanceChains?.LastOrDefault().Values.Select(uid => UidToTypeMDString(uid, _store)).ToList();
            }
            
            sdpEnum.IsFlags = enumTypeItem.Attributes != null 
                && enumTypeItem.Attributes.Any(attr => attr.Declaration == "System.Flags");

            sdpEnum.Fields = enumTypeItem.Members.Select(fItem =>
            {
                var f = new EnumField()
                {
                    Uid = fItem.Uid,
                    CommentId = fItem.CommentId,
                    Name = fItem.DisplayName,
                    NameWithType = enumTypeItem.Name + '.' + fItem.Name,
                    FullName = fItem.FullDisplayName,
                    Summary = fItem.Docs.Summary,
                    Monikers = fItem.Monikers
                };
                if (fItem.Metadata.TryGetValue(OPSMetadata.LiteralValue, out object val))
                {
                    f.LiteralValue = val?.ToString();
                }
                f.LiteralValue = f.LiteralValue ?? "";
                memberTouchCache.Add(f.Uid);

                return f;
            }).ToList().NullIfEmpty();

            return sdpEnum;
        }
    }
}
