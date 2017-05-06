using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public class OPSMetadata
    {
        public static readonly string Monikers = "monikers";
        public static readonly string Version = "version";
        public static readonly string ContentUrl = "content_git_url";
        public static readonly string ThreadSafety = "thread_safety";
        public static readonly string AdditionalNotes_Format = "additional_notes.{0}";
        public static readonly string XMLLocalPath = "original_ecmaxml_local_path";
        public static readonly string AltCompliant = "altCompliant";
        public static readonly string InternalOnly = "internal_use_only";
        public static readonly string NugetPackageNames = "nuget_package_names";
        public static readonly string OpenToPublic = "open_to_public_contributors";

        public static readonly string Universal_Conceptual_TOC = "universal_conceptual_toc";
        public static readonly string Universal_Ref_TOC = "universal_ref_toc";
    }
    public enum SyntaxLanguage
    {
        Default = 0,
        CSharp = 1,
        CPlusPlus = 2,
        FSharp = 3,
        Javascript = 4,
        VB = 5
    }
}
