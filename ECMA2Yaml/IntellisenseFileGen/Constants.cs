using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntellisenseFileGen
{
    public class Constants
    {
        // [!INCLUDE[vstecmsbuild] (~/includes/vstecmsbuild-md.md)]
        public static readonly string Include_Pattern1 = "(\\[!INCLUDE.*?\\((.*?)\\)\\])";
        // !INCLUDE[linq_dataset]
        public static readonly string Include_Pattern2 = "(!INCLUDE\\[(.*?)\\])";

        // [ISymUnmanagedWriter Interface](~/docs/framework/unmanaged-api/diagnostics/isymunmanagedwriter-interface.md) 
        public static readonly string Link_Pattern = "(\\[(.*?)\\]\\(.*\\))";

        // *Unix*,_Unix_
        public static readonly string SingleSytax_Pattern = "([\\*|\\`]([\\w|\\.|\\#|\\+|\\s|/|-]+?)[\\*|\\`])";
        // **Unix**,__Unix__
        public static readonly string DoubleSytax_Pattern = "([_*]{2}([\\w|\\.|\\#|\\+|\\s|/|-]+?)[_*]{2})";
        // 
        public static readonly string TripleSytax_Pattern = "TODO";

    }
}
