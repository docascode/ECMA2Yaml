using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.IO
{
    public class FileItem
    {
        public string RelativePath { get; set; }

        public string AbsolutePath { get; set; }

        public bool IsVirtual { get; set; }
    }
}
