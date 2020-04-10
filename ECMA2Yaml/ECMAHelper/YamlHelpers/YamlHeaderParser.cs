using Microsoft.DocAsCode.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace ECMA2Yaml
{
    public class YamlHeaderParser
    {
        public static readonly Regex YamlHeaderRegex = new Regex(@"\-{3}(?:\s*?)\n([\s\S]+?)(?:\s*?)\n\-{3}(?:\s*?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(2));

        public static Dictionary<string, Dictionary<string, object>> LoadOverwriteMetadata(string path)
        {
            DirectoryInfo info = new DirectoryInfo(path);
            return WalkDirectoryTree(info);
        }

        static Dictionary<string, Dictionary<string, object>> WalkDirectoryTree(DirectoryInfo root)
        {
            FileInfo[] files = null;
            DirectoryInfo[] subDirs = null;
            Dictionary<string, Dictionary<string, object>> rval = new Dictionary<string, Dictionary<string, object>>();

            files = root.GetFiles("*.md");

            if (files != null)
            {
                foreach (FileInfo fi in files)
                {
                    MergeMetadataDict(rval, LoadFile(fi.FullName));
                }

                subDirs = root.GetDirectories();

                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    MergeMetadataDict(rval, WalkDirectoryTree(dirInfo));
                }
            }

            return rval;
        }

        static Dictionary<string, Dictionary<string, object>> LoadFile(string path)
        {
            var rval = new Dictionary<string, Dictionary<string, object>>();
            var text = File.ReadAllText(path);
            var matches = YamlHeaderRegex.Matches(text);
            if (matches != null && matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    using (StringReader reader = new StringReader(match.Groups[1].Value))
                    {
                        Dictionary<string, object> result = null;
                        try
                        {
                            result = YamlUtility.Deserialize<Dictionary<string, object>>(reader);
                        }
                        catch (Exception ex)
                        {
                            OPSLogger.LogUserError(LogCode.ECMA2Yaml_YamlHeader_ParseFailed_WithException, path, ex);
                        }
                        if (result == null)
                        {
                            OPSLogger.LogUserError(LogCode.ECMA2Yaml_YamlHeader_ParseFailed, path, match.Value);
                        }
                        else if (!result.ContainsKey("uid"))
                        {
                            OPSLogger.LogUserError(LogCode.ECMA2Yaml_YamlHeader_ParseFailed, path, match.Value);
                        }
                        else
                        {
                            var uid = result["uid"].ToString();
                            result.Remove("uid");
                            if (rval.ContainsKey(uid))
                            {
                                OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_Uid_Duplicated, path, uid);
                            }
                            else
                            {
                                rval[uid] = result;
                            }
                        }
                    }
                }
            }

            return rval;
        }

        static void MergeMetadataDict(Dictionary<string, Dictionary<string, object>> left, Dictionary<string, Dictionary<string, object>> right)
        {
            foreach(var uid in right)
            {
                if (!left.ContainsKey(uid.Key))
                {
                    left.Add(uid.Key, uid.Value);
                }
                else
                {
                    foreach(var pair in uid.Value)
                    {
                        left[uid.Key][pair.Key] = pair.Value;
                    }
                }
            }
        }
    }
}
