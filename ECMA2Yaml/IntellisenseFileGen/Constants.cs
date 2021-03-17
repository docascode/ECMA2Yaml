using System.Text.RegularExpressions;

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

        // *Unix*
        public static Regex SingleSytax_Pattern1 = new Regex("(\\*([\\w|\\.|\\#|\\+|\\s|/|:|-]+?)\\*)", RegexOptions.Compiled);
        // `Unix`
        public static Regex SingleSytax_Pattern2 = new Regex("(`(.*?)`)", RegexOptions.Compiled);
        // **Unix**,__Unix__
        public static Regex DoubleSytax_Pattern = new Regex("([_*]{2}([\\w|\\.|\\#|\\+|\\s|/|:|-]+?)[_*]{2})", RegexOptions.Compiled);

        //```csharp this is a test page```
        //```csharp 
        //this is a test page 
        //```
        public static Regex TripleSytax_Pattern1 = new Regex("(```csharp\\s*(.*?)\\s*```)", RegexOptions.Compiled);

        //```this is a test page```
        //```
        //this is a test page
        //```
        public static Regex TripleSytax_Pattern2 = new Regex("(```\\s*(.*?)\\s*```)", RegexOptions.Compiled);

        // <summary>Creates an @Windows.AI.MachineLearning.ImageFeatureValue?text=ImageFeatureValue using the given video frame.</summary>
        public static Regex Link_Pattern1 = new Regex("(@.*?\\?text=(\\w*)\"?)", RegexOptions.Compiled);
    }
}
