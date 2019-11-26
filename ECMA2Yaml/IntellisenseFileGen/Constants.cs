using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IntellisenseFileGen
{
    public class Constants
    {
        // [!INCLUDE[vstecmsbuild] (~/includes/vstecmsbuild-md.md)]
        public static Regex Include_Pattern1 = new Regex("(\\[!INCLUDE.*?\\((.*?)\\)\\])", RegexOptions.Compiled);
        // !INCLUDE[linq_dataset]
        public static Regex Include_Pattern2 = new Regex("(!INCLUDE\\[(.*?)\\])", RegexOptions.Compiled);

        // [ISymUnmanagedWriter Interface](~/docs/framework/unmanaged-api/diagnostics/isymunmanagedwriter-interface.md) 
        public static Regex Link_Pattern = new Regex("(\\[(.*?)\\]\\(.*\\))", RegexOptions.Compiled);

        // *Unix*,_Unix_
        public static Regex SingleSytax_Pattern = new Regex("([\\*|\\`]([\\w|\\.|\\#|\\+|\\s|/|-]+?)[\\*|\\`])", RegexOptions.Compiled);
        // **Unix**,__Unix__
        public static Regex DoubleSytax_Pattern = new Regex("([_*]{2}([\\w|\\.|\\#|\\+|\\s|/|-]+?)[_*]{2})", RegexOptions.Compiled);
        // 
        public static Regex TripleSytax_Pattern = new Regex("TODO", RegexOptions.Compiled);

    }
}
