using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.UndocumentedApi.Models
{
    public class ReportItem
    {
        public string Uid { get; set; }
        public string CommentId { get; set; }
        public string Type { get; set; }
        public string SourceFilePath { get; set; }
        public string Url { get; set; }

        public string Namespace { get; set; }
        public string Class { get; set; }
        public string Name { get; set; }

        public Dictionary<FieldType, ValidationResult> Results { get; set; }
    }

    public class ReportItemComparer : IComparer<ReportItem>
    {

        public int Compare(ReportItem itemA, ReportItem itemB)
        {
            var nsResult = itemA.Namespace.CompareTo(itemB.Namespace);
            if (nsResult != 0)
            {
                return nsResult;
            }
            var classResult = itemA.Class.CompareTo(itemB.Class);
            if (classResult != 0)
            {
                return classResult;
            }
            return itemA.Name.CompareTo(itemB.Name);
        }
    }
}
