using ECMA2Yaml.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ECMA2Yaml
{
    public partial class ECMALoader
    {
        public XElement TransformDocs(XElement dElement)
        {
            if (dElement == null)
            {
                return null;
            }

            XElement remarks = dElement.Element("remarks");
            bool skipRemarks = remarks?.Element("format") != null;
            if (remarks != null && skipRemarks)
            {
                remarks.Remove();
            }

            var dElement2 = _docsTransform.Transform(dElement.ToString()).Root;

            if (remarks != null && skipRemarks)
            {
                dElement2.Add(remarks);
            }
            return dElement2;
        }

        public Docs LoadDocs(XElement dElement)
        {
            dElement = TransformDocs(dElement);
            if (dElement == null)
            {
                return null;
            }

            var remarks = dElement.Element("remarks");
            string remarksText = null;
            string examplesText = null;
            if (remarks?.Element("format") != null)
            {
                remarksText = remarks.Element("format").Value;
            }
            else
            {
                remarksText = NormalizeDocsElement(remarks, true);
            }
            if (remarksText != null)
            {
                remarksText = remarksText.Replace("## Remarks", "").Trim();
                if (remarksText.Contains("## Examples"))
                {
                    var pos = remarksText.IndexOf("## Examples");
                    examplesText = remarksText.Substring(pos).Replace("## Examples", "").Trim();
                    remarksText = remarksText.Substring(0, pos).Trim();
                }
            }

            var examples = dElement.Elements("example");
            if (examples != null && examples.Count() > 0)
            {
                examplesText = string.IsNullOrEmpty(examplesText) ? "" : examplesText + "\n\n";
                examplesText += string.Join("\n\n", examples.Select(example => NormalizeDocsElement(example)));
            }

            List<RelatedTag> related = null;
            var relatedElements = dElement.Elements("related")?.ToList();
            if (relatedElements?.Count > 0)
            {
                related = LoadRelated(relatedElements);
            }

            Dictionary<string, string> additionalNotes = null;
            var blocks = dElement.Elements("block")?.Where(p => !string.IsNullOrEmpty(p.Attribute("type").Value)).ToList();
            if (blocks != null && blocks.Count > 0)
            {
                additionalNotes = new Dictionary<string, string>();
                foreach (var block in blocks)
                {
                    var valElement = block;
                    var elements = block.Elements().ToArray();
                    if (elements?.Length == 1 && elements[0].Name.LocalName == "p")
                    {
                        valElement = elements[0];
                    }
                    additionalNotes[block.Attribute("type").Value] = NormalizeDocsElement(GetInnerXml(valElement));
                }
            }

            string altCompliant = dElement.Element("altCompliant")?.Attribute("cref")?.Value;
            if (!string.IsNullOrEmpty(altCompliant) && altCompliant.Contains(":"))
            {
                altCompliant = altCompliant.Substring(altCompliant.IndexOf(':') + 1);
            }

            string threadSafetyContent = null;
            ThreadSafety threadSafety = null;
            var threadSafeEle = dElement.Element("threadsafe");
            if (threadSafeEle != null)
            {
                threadSafetyContent = NormalizeDocsElement(GetInnerXml(threadSafeEle));
                var supportedAttr = threadSafeEle.Attribute("supported");
                if (supportedAttr != null)
                {
                    threadSafety = new ThreadSafety()
                    {
                        CustomContent = threadSafetyContent,
                        Supported = supportedAttr.Value?.ToLower() == "true",
                        MemberScope = threadSafeEle.Attribute("memberScope")?.Value
                    };
                }
            }

            return new Docs()
            {
                Summary = NormalizeDocsElement(dElement.Element("summary")),
                Remarks = remarksText,
                Examples = examplesText,
                AltMemberCommentIds = dElement.Elements("altmember")?.Select(alt => alt.Attribute("cref").Value).ToList(),
                Related = related,
                Exceptions = dElement.Elements("exception")?.Select(el => GetTypedContent(el)).ToList(),
                Permissions = dElement.Elements("permission")?.Select(el => GetTypedContent(el)).ToList(),
                Parameters = dElement.Elements("param")?.Where(p => !string.IsNullOrEmpty(p.Attribute("name").Value)).ToDictionary(p => p.Attribute("name").Value, p => NormalizeDocsElement(GetInnerXml(p))),
                TypeParameters = dElement.Elements("typeparam")?.Where(p => !string.IsNullOrEmpty(p.Attribute("name").Value)).ToDictionary(p => p.Attribute("name").Value, p => NormalizeDocsElement(GetInnerXml(p))),
                AdditionalNotes = additionalNotes,
                Returns = NormalizeDocsElement(GetInnerXml(dElement.Element("returns"))), //<value> will be transformed to <returns> by xslt in advance
                ThreadSafety = threadSafetyContent,
                ThreadSafetyInfo = threadSafety,
                Since = NormalizeDocsElement(dElement.Element("since")?.Value),
                AltCompliant = altCompliant,
                InternalOnly = dElement.Element("forInternalUseOnly") != null
            };
        }

        private List<RelatedTag> LoadRelated(List<XElement> relatedElements)
        {
            if (relatedElements == null)
            {
                return null;
            }
            var tags = new List<RelatedTag>();
            foreach(var element in relatedElements)
            {
                var href = element.Attribute("href")?.Value;
                if (!string.IsNullOrEmpty(href))
                {
                    var tag = new RelatedTag()
                    {
                        Uri = href,
                        Text = element.Value
                    };
                    var type = element.Attribute("type")?.Value;
                    if (!string.IsNullOrEmpty(type))
                    {
                        tag.Type = (RelatedType)Enum.Parse(typeof(RelatedType), type, true);
                    }
                    tags.Add(tag);
                }
            }
            return tags;
        }

        private TypedContent GetTypedContent(XElement ele)
        {
            var cref = ele.Attribute("cref").Value;
            return new TypedContent
            {
                CommentId = cref,
                Description = NormalizeDocsElement(GetInnerXml(ele)),
                Uid = cref.Substring(cref.IndexOf(':') + 1).Replace('+', '.')
            };
        }

        private static string GetInnerXml(XElement ele)
        {
            if (ele == null)
            {
                return null;
            }
            var reader = ele.CreateReader();
            reader.MoveToContent();
            return reader.ReadInnerXml();
        }

        //private static Regex xrefFix = new Regex("<xref:[\\w\\.\\d\\?=]+%[\\w\\.\\d\\?=%]+>", RegexOptions.Compiled);
        private static Regex tagDetect = new Regex("<[^>]*>", RegexOptions.Compiled);
        private static string NormalizeDocsElement(XElement ele, bool wrap = false)
        {
            if (ele?.Element("format") != null)
            {
                return ele.Element("format").Value;
            }
            else
            {
                var innerXml = GetInnerXml(ele);
                if (string.IsNullOrEmpty(innerXml) || innerXml.Trim() == "To be added.")
                {
                    return null;
                }
                innerXml = NormalizeIndent(innerXml, out bool formatDetected);
                if (wrap && formatDetected && !tagDetect.IsMatch(innerXml))
                {
                    innerXml = string.Format("<pre>{0}</pre>", innerXml);
                }
                return NormalizeDocsElement(innerXml);
            }
        }

        private static string NormalizeDocsElement(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            var trimmed = str.Trim();

            return trimmed == "To be added." ? null : trimmed;
        }

        private static string NormalizeIndent(string str, out bool formatDetected)
        {
            int minIndent = int.MaxValue;
            var lines = str.TrimStart('\r', '\n').TrimEnd().Split('\r', '\n');
            if (lines.Length == 1)
            {
                formatDetected = false;
                return lines[0].Trim();
            }
            foreach (var line in lines)
            {
                var indent = 0;
                while(indent < line.Length && char.IsWhiteSpace(line[indent]))
                {
                    indent++;
                }
                minIndent = Math.Min(minIndent, indent);
            }
            formatDetected = true;
            return string.Join("\n", lines.Select(l => l.Length >= minIndent ? l.Substring(minIndent) : l));
        }
    }
}
