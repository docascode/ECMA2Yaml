using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.UndocumentedApi.Models
{
    public class Report
    {
        public string Repository { get; set; }
        public string Branch { get; set; }
        public List<ReportItem> ReportItems { get; set; }
    }
}
