using ECMA2Yaml.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
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

            var dElement2 = _docsTransform.Transform(dElement.ToString()).Root;

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
            string remarksText = NormalizeDocsElement(remarks, true);
            string examplesText = null;
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

            remarksText = DowngradeMarkdownHeaders(remarksText);

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
            var blocks = dElement.Elements("block")?.Where(p => !string.IsNullOrEmpty(p.Attribute("type")?.Value)).ToList();
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

            string threadSafetyContent = null;
            ThreadSafety threadSafety = null;
            var threadSafeEle = dElement.Element("threadsafe");
            if (threadSafeEle != null)
            {
                threadSafetyContent = NormalizeDocsElement(GetInnerXml(threadSafeEle));
                var supportedAttr = threadSafeEle.Attribute("supported");
                threadSafety = new ThreadSafety()
                {
                    CustomContent = threadSafetyContent,
                    Supported = supportedAttr?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase),
                    MemberScope = threadSafeEle.Attribute("memberScope")?.Value
                };
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
                Parameters = dElement.Elements("param")?.Where(p => !string.IsNullOrEmpty(p.Attribute("name").Value)).ToDictionary(p => p.Attribute("name").Value, p => NormalizeDocsElement(p)),
                TypeParameters = dElement.Elements("typeparam")?.Where(p => !string.IsNullOrEmpty(p.Attribute("name").Value)).ToDictionary(p => p.Attribute("name").Value, p => NormalizeDocsElement(GetInnerXml(p))),
                AdditionalNotes = additionalNotes,
                Returns = NormalizeDocsElement(dElement.Element("returns")), //<value> will be transformed to <returns> by xslt in advance
                ThreadSafety = threadSafetyContent,
                ThreadSafetyInfo = threadSafety,
                Since = NormalizeDocsElement(dElement.Element("since")?.Value),
                AltCompliant = dElement.Element("altCompliant")?.Attribute("cref")?.Value,
                InternalOnly = dElement.Element("forInternalUseOnly") != null
            };
        }

        /// <summary>Downgrades markdown headers from 1 - 5. So a `#` becomes `##`, but `######` (ie. h6) remains the same.</summary>
        /// <param name="remarksText">A string of markdown content</param>
        public static string DowngradeMarkdownHeaders(string remarksText)
        {
            if (string.IsNullOrWhiteSpace(remarksText)) return remarksText;

            // only trigger behavior if there's an H2 in the text
            if (!markdownH2HeaderRegex.IsMatch(remarksText))
            {
                return remarksText;
            }

            var lines = remarksText.Split(new[] { '\n' }, StringSplitOptions.None);

            bool replaceTriggered = false;

            // walk through the content, first adjusting larger headers and moving in reverse
            for (int headerSize = 5; headerSize > 0; headerSize--)
                ReplaceTriggered(lines, headerSize, ref replaceTriggered);
            
            return replaceTriggered ? string.Join("\n", lines) : remarksText;
        }

        private static readonly string[] markdownHeaders = new string []
        {
            "#",
            "##",
            "###",
            "####",
            "#####",
            "######"
        };
        private static readonly Regex markdownH2HeaderRegex = new Regex("^\\s{0,3}##[^#]", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>Determines whether the string is a markdown header (or at least, starts with one ... it assumes this is a single line of text)</summary>
        /// <param name="line">an individual line of a markdown document</param>
        /// <param name="headerSize">the 'level' of header. So '2' is an 'H2'.</param>
        /// <returns>True if this is a markdown header that matches the headerSize. It allows for up to 3 spaces in front of the pound signs</returns>
        private static bool IsHeader(string line, int headerSize)
        {
            int whitespaceCount = 0;
            int hashCount = 0;
            bool breakLoop = false;
            for (int i = 0; i < line.Length; i++)
            {
                switch(line[i])
                {
                    case ' ':
                        if (hashCount > 0)
                        {
                            breakLoop = true;
                            break;
                        }
                        whitespaceCount++;
                        break;
                    case '#':
                        hashCount++;
                        break;
                    default:
                        breakLoop = true;
                        break;
                }

                if (breakLoop)
                    break;
            }

            if (whitespaceCount < 4 && hashCount == headerSize)
                return true;
            else
                return false;
        }

        /// <summary>Modifies the array if a header of the given size is found</summary>
        private static void ReplaceTriggered(string[] lines, int headerCount, ref bool replaceTriggered)
        {
            string headerPrefix = markdownHeaders[headerCount-1];
            string newHeaderPrefix = null;
            bool inCodeFence = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.StartsWith("```"))
                    inCodeFence = !inCodeFence;// invert codefence flag

                // this allows for documentation about markdown
                if (inCodeFence)
                    continue;

                if (IsHeader(line, headerCount))
                {
                    if (newHeaderPrefix == null)
                        newHeaderPrefix = markdownHeaders[headerCount];
                    lines[i] = line.Replace(headerPrefix, newHeaderPrefix);
                    replaceTriggered = true;
                }
            }
        }

        private List<RelatedTag> LoadRelated(List<XElement> relatedElements)
        {
            if (relatedElements == null)
            {
                return null;
            }
            var tags = new List<RelatedTag>();
            foreach (var element in relatedElements)
            {
                var href = element.Attribute("href")?.Value;
                if (!string.IsNullOrEmpty(href))
                {
                    var tag = new RelatedTag()
                    {
                        Uri = href,
                        Text = element.Value,
                        OriginalText = GetInnerXml(element)
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

        private static string NormalizeDocsElement(XElement ele, bool wrap = false)
        {
            if (ele == null)
            {
                return null;
            }
            else if (ele.Element("format") != null && ele.Elements().Count() == 1) // markdown
            {
                return NormalizeTextIndent(ele.Element("format").Value, out _);
            }
            else if (ele.HasElements) // comment xml
            {
                var val = GetInnerXml(ele);
                val = RemoveIndentFromXml(val);
                return val;
            }
            else // plain text content
            {
                var val = GetInnerXml(ele);
                if (string.IsNullOrEmpty(val) || val.Trim() == "To be added.")
                {
                    return null;
                }
                val = NormalizeTextIndent(val, out bool formatDetected);
                if (wrap && formatDetected)
                {
                    //val = string.Format("<pre>{0}</pre>", val);
                    val = val.Replace("\n", "\n\n");
                }
                return val;
            }
        }

        private static string NormalizeDocsElement(string str)
        {
            str = NormalizeTextIndent(str, out _);
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            var trimmed = str.Trim();

            return trimmed == "To be added." ? null : trimmed;
        }

        private static string NormalizeTextIndent(string str, out bool formatDetected)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                formatDetected = false;
                return str;
            }
            int minIndent = int.MaxValue;
            var lines = str.TrimEnd().Split('\r', '\n');
            var startIndex = 0;
            while (string.IsNullOrWhiteSpace(lines[startIndex]))
            {
                startIndex++;
            }
            if (startIndex == lines.Length - 1)
            {
                formatDetected = false;
                return lines[startIndex].Trim();
            }
            for (int i = startIndex; i < lines.Length; i++)
            {
                var indent = 0;
                while (indent < lines[i].Length && char.IsWhiteSpace(lines[i][indent]))
                {
                    indent++;
                }
                minIndent = Math.Min(minIndent, indent);
            }
            formatDetected = true;
            return string.Join("\n", lines.Skip(startIndex).Select(l => l.Length >= minIndent ? l.Substring(minIndent) : l));
        }

        private static readonly Regex XmlIndentRegex = new Regex("^[\\t ]+<", RegexOptions.Multiline | RegexOptions.Compiled);
        private static string RemoveIndentFromXml(string str)
        {
            var tmp = NormalizeTextIndent(str, out _);
            if (str.StartsWith("<") || str.TrimStart().StartsWith("<"))
            {
                return XmlIndentRegex.Replace(tmp, "<").Trim();
            }
            return tmp;
        }

        private static XElement NormalizeXMLIndent(XElement element)
        {
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "",
                OmitXmlDeclaration = false
            };
            element = XElement.Parse(element.ToString(SaveOptions.DisableFormatting));
            using (var sw = new StringWriter())
            {
                using (var writer = XmlWriter.Create(sw, settings))
                {
                    element.Save(writer);
                }
                return XElement.Parse(sw.ToString(), LoadOptions.PreserveWhitespace);
            }
        }
    }
}
