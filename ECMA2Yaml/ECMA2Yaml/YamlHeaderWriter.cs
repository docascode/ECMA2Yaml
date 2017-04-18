using ECMA2Yaml.Models;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Common.EntityMergers;
using Microsoft.DocAsCode.DataContracts.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ECMA2Yaml
{
    public class Overload
    {
        public Overload() { }

        [JsonProperty("summary")]
        [MarkdownContent]
        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [JsonProperty("remarks")]
        [MarkdownContent]
        [YamlMember(Alias = "remarks")]
        public string Remarks { get; set; }

        [JsonProperty("example")]
        [MergeOption(MergeOption.Replace)]
        [YamlMember(Alias = "example")]
        public List<string> Examples { get; set; }

        [JsonProperty("uid")]
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }
    }

    public class YamlHeaderWriter
    {
        public static string GenerateOverwriteBlock(object headerModel)
        {
            StringWriter sw = new StringWriter();
            sw.WriteLine("---");
            YamlUtility.Serialize(sw, headerModel);
            sw.WriteLine("---");
            return sw.ToString();
        }

        public static string GenerateOverwriteBlockForMarkup(string uid, string metadataName, string mdContent)
        {
            StringWriter sw = new StringWriter();
            sw.WriteLine("---");
            sw.WriteLine("uid: {0}", uid);
            sw.WriteLine("{0}: {1}", metadataName, "*content");
            sw.WriteLine("---");
            sw.WriteLine();
            sw.WriteLine(mdContent);
            sw.WriteLine();
            return sw.ToString();
        }

        public static void WriteOverload(Member overload, string folder)
        {
            var model = new Overload
            {
                Uid = overload.Uid
            };
            if (!string.IsNullOrEmpty(overload.Docs.Summary))
            {
                model.Summary = overload.Docs.Summary;
            }
            if (!string.IsNullOrEmpty(overload.Docs.Remarks))
            {
                model.Remarks = overload.Docs.Remarks;
            }
            if (!string.IsNullOrEmpty(overload.Docs.Examples))
            {
                model.Examples = new List<string> { overload.Docs.Examples };
            }
            var fileContent = GenerateOverwriteBlock(model);

            string fileName = null;
            try
            {
                fileName = Path.Combine(folder, overload.Uid.Replace("*", "_") + ".md");
                File.WriteAllText(fileName, fileContent);
            }
            catch (Exception ex)
            {
                OPSLogger.LogUserError("Unable to save overload md file for " + overload.Uid);
                return;
            }
        }

        public static void WriteCustomContentIfAny(string uid, Docs docs, string folder)
        {
            List<string> blocks = new List<string>();
            if (!string.IsNullOrEmpty(docs.ThreadSafety))
            {
                blocks.Add(GenerateOverwriteBlockForMarkup(uid, OPSMetadata.ThreadSafety, docs.ThreadSafety.TrimEnd()));
            }
            if (docs.AdditionalNotes != null)
            {
                foreach(var note in docs.AdditionalNotes)
                {
                    blocks.Add(GenerateOverwriteBlockForMarkup(uid, string.Format(OPSMetadata.AdditionalNotes_Format, note.Key), note.Value.TrimEnd()));
                }
            }

            string fileName = null;
            if (blocks.Count > 0)
            {
                try
                {
                    fileName = Path.Combine(folder, TruncateUid(uid.Replace("*", "_")) + ".misc.md");
                    File.WriteAllLines(fileName, blocks);
                }
                catch (Exception ex)
                {
                    OPSLogger.LogUserError("Unable to save content overwrite md file for " + uid);
                    return;
                }
            }
        }

        private static string TruncateUid(string uid)
        {
            if (uid.Length <= 180)
            {
                return uid;
            }
            return uid.Substring(0, 180) + uid.GetHashCode();
        }
    }
}
