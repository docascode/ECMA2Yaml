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
        public static void WriteFile(object headerModel, string filePath)
        {
            StringWriter sw = new StringWriter();
            sw.WriteLine("---");
            YamlUtility.Serialize(sw, headerModel);
            sw.WriteLine("---");
            File.WriteAllText(filePath, sw.ToString());
        }

        public static void WriterOverload(Member overload, string folder)
        {
            string fileName = null;
            try{
            fileName = Path.Combine(folder, overload.Uid.Replace("*", "_").Replace("?", "_") + ".md");
            }catch(Exception ex)
            {
                OPSLogger.LogUserError("Unable to save overload md file for " + overload.Uid);
                return;
            }
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
            WriteFile(model, fileName);
        }
    }
}
