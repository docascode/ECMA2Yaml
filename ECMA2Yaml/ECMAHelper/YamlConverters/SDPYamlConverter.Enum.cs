﻿using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Collections.Generic;
using System.Linq;
using Type = ECMA2Yaml.Models.Type;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        public EnumSDPModel FormatEnum(Type enumTypeItem, HashSet<string> memberTouchCache)
        {
            var sdpEnum = InitWithBasicProperties<EnumSDPModel>(enumTypeItem);

            sdpEnum.InheritancesWithMoniker = ConverterHelper.TrimMonikers(
                enumTypeItem.InheritanceChains?.Select(
                chain => new VersionedCollection<string>(
                    chain.Monikers,
                    chain.Values.Select(uid => UidToTypeMDString(uid, _store)).ToList()
                    ){ ValuesPerLanguage= CovnertNamedInheritancesWithMonikerPerLanguage(chain.Values, enumTypeItem) }).ToList(),
                enumTypeItem.Monikers);

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

        private List<VersionedString> CovnertNamedInheritancesWithMonikerPerLanguage(List<string> list, Type enumTypeItem)
        {
            if (list == null)
            {
                return null;
            }

            var versionedList = new List<VersionedString>();
            list.ForEach(uid => {
                var value = UidToTypeMDString(uid, _store);
                versionedList.Add(new VersionedString() { Value = UidToTypeMDString(uid, _store), PerLanguage = ConvertNamedPerLanguage(GetTypNameByUid(uid), enumTypeItem, true) });
            });

            if (versionedList.Any(item => item.PerLanguage != null))
            {
                return versionedList;
            }

            return null;
        }
    }
}
