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
        public static readonly string ContentUrl = "content_git_url";
        public static readonly string OriginalContentUrl = "original_content_git_url";
        public static readonly string RefSkeletionUrl = "original_ref_skeleton_git_url";
        public static readonly string ThreadSafety = "thread_safety";
        public static readonly string ThreadSafetyInfo = "thread_safety_info";
        public static readonly string AdditionalNotes = "additionalNotes";
        public static readonly string Permissions = "permissions";
        public static readonly string AltCompliant = "altCompliant";
        public static readonly string InternalOnly = "internal_use_only";
        public static readonly string NugetPackageNames = "nuget_package_names";
        public static readonly string OpenToPublic = "open_to_public_contributors";
        public static readonly string LiteralValue = "literalValue";
        public static readonly string AssemblyMonikerMapping = "_op_AssemblyMonikerMapping";

        public static readonly string HelpViewerKeywords = "helpviewer_keywords";
        public static readonly string F1Keywords = "f1_keywords";

        public static readonly string SDP_op_articleFileGitUrl = "_op_articleFileGitUrl";
        public static readonly string SDP_op_overwriteFileGitUrl = "_op_overwriteFileGitUrl";
    }

    public static class Constants
    {
        public static IReadOnlyDictionary<string, string> DevLangMapping = new Dictionary<string, string>
        {
            {"C#", "csharp" },
            {"VB.NET", "vb" },
            {"F#", "fsharp" },
            {"C++ CLI", "cpp" },
            {"C++ CX", "cppcx" },
            {"C++ WINRT", "cppwinrt" }
        };
    }
}
