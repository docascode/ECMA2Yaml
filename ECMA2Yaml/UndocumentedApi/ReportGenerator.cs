using ECMA2Yaml.Models;
using ECMA2Yaml.UndocumentedApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.UndocumentedApi
{
    public class ReportGenerator
    {
        public static void GenerateReport(ECMAStore store)
        {
            List<ReportItem> items = new List<ReportItem>();
            items.AddRange(store.Namespaces.Values.Where(ns => ns.Uid != null).Select(ns => ValidateItem(ns)));
            items.AddRange(store.TypesByUid.Values.Select(t => ValidateItem(t)));
            items.AddRange(store.MembersByUid.Values.Select(m => ValidateItem(m)));
            items.Sort(new ReportItemComparer());


        }

        private static ReportItem ValidateItem(ReflectionItem item)
        {
            ReportItem report = new ReportItem()
            {
                Uid = item.Uid,
                CommentId = item.CommentId,
                Type = item.ItemType.ToString(),
                Name = item.Name,
                Results = Validator.ValidateItem(item),
                SourceFilePath = item.SourceFileLocalPath
            };
            switch (item)
            {
                case ECMA2Yaml.Models.Namespace ns:
                    report.Namespace = ns.Name;
                    report.Class = "";
                    break;
                case ECMA2Yaml.Models.Type t:
                    report.Namespace = t.Parent?.Name;
                    report.Class = t.Name;
                    break;
                case ECMA2Yaml.Models.Member m:
                    report.Namespace = m.Parent?.Parent?.Name;
                    report.Class = m.Parent?.Name;
                    break;
            }

            return report;
        }
    }
}
