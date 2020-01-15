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

    public class UWPMetadata
    {
        public static readonly string RequirementSDKNames = "requirement_sdk_names";
        public static readonly string RequirementSDKUrls = "requirement_sdk_urls";
        public static readonly string RequirementOSNames = "requirement_os_names";
        public static readonly string RequirementOSMinVersions = "requirement_os_min_versions";
        public static readonly string DeviceFamilies = "deviceFamilies";
        public static readonly string DeviceFamiliesVersions = "deviceFamiliesVersions";
        public static readonly string ApiContracts = "apiContracts";
        public static readonly string ApiContractsVersions = "apiContractsVersions";
        public static readonly string Capabilities = "capabilities";
        public static readonly string XamlMemberSyntax = "xamlMemberSyntax";
        public static readonly string ContentSourcePath = "contentSourcePath";

        public static readonly Dictionary<string, MetadataDataType> Values = new Dictionary<string, MetadataDataType>
        {
            {RequirementSDKNames, MetadataDataType.StringArray },
            {RequirementSDKUrls, MetadataDataType.StringArray },
            {RequirementOSNames, MetadataDataType.StringArray },
            {RequirementOSMinVersions, MetadataDataType.StringArray },
            {DeviceFamilies, MetadataDataType.StringArray },
            {DeviceFamiliesVersions, MetadataDataType.StringArray },
            {ApiContracts, MetadataDataType.StringArray },
            {ApiContractsVersions, MetadataDataType.StringArray },
            {Capabilities, MetadataDataType.StringArray },
            {XamlMemberSyntax, MetadataDataType.String },
            {ContentSourcePath, MetadataDataType.String }
        };
    }

    public enum MetadataDataType
    {
        String,
        StringArray
    }

    public static class ECMADevLangs
    {
        public static IReadOnlyDictionary<string, string> OPSMapping = new Dictionary<string, string>
        {
            { CSharp, "csharp" },
            { VB, "vb" },
            { FSharp, "fsharp" },
            { CPP_CLI, "cpp" },
            { CPP_CX, "cppcx" },
            { CPP_WINRT, "cppwinrt" },
            { JavaScript, "javascript" }
        };

        public static readonly string[] All = typeof(ECMADevLangs).GetAllPublicConstantValues<string>();

        public const string CSharp = "C#";
        public const string VB = "VB.NET";
        public const string FSharp = "F#";
        public const string CPP_CLI = "C++ CLI";
        public const string CPP_CX = "C++ CX";
        public const string CPP_WINRT = "C++ WINRT";
        public const string JavaScript = "JavaScript";
    }
}
