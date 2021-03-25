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

        public Docs LoadDocs(XElement dElement, string filePath)
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
                if (remarksText.Contains("</format>"))
                {
                    Console.WriteLine(filePath);
                    Console.WriteLine(remarksText);
                }
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

            var relatedElements =
                dElement.Elements("related")
                .Concat(dElement.Elements("seealso").Where(element => element.Attribute("href") != null && element.Attribute("cref") == null))
                .ToList();

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

            var inheritdocEle = dElement.Element("inheritdoc");
            InheritDoc inheritDoc = null;
            if (inheritdocEle != null)
            {
                inheritDoc = new InheritDoc();
                var inheritCref = inheritdocEle?.Attribute("cref")?.Value;
                var inheritPath = inheritdocEle?.Attribute("path")?.Value;
                if (string.IsNullOrEmpty(inheritCref))
                {
                    inheritDoc.Cref = inheritCref;
                }
                if (string.IsNullOrEmpty(inheritPath))
                {
                    inheritDoc.Path = inheritPath;
                }
            }

            return new Docs()
            {
                Summary = NormalizeDocsElement(dElement.Element("summary")),
                Remarks = remarksText,
                Examples = examplesText,
                AltMemberCommentIds = MergeAltmemberAndSeealsoToAltMemberCommentsIds(dElement),//dElement.Elements("altmember")?.Select(alt => alt.Attribute("cref").Value).ToList(),
                Related = related,
                Exceptions = dElement.Elements("exception")?.Select(el => GetTypedContent(el, filePath)).ToList(),
                Permissions = dElement.Elements("permission")?.Select(el => GetTypedContent(el, filePath)).ToList(),
                Parameters = dElement.Elements("param")?.Where(p => !string.IsNullOrEmpty(p.Attribute("name").Value)).ToDictionary(p => p.Attribute("name").Value, p => NormalizeDocsElement(p)),
                TypeParameters = dElement.Elements("typeparam")?.Where(p => !string.IsNullOrEmpty(p.Attribute("name").Value)).ToDictionary(p => p.Attribute("name").Value, p => NormalizeDocsElement(GetInnerXml(p))),
                AdditionalNotes = additionalNotes,
                Returns = NormalizeDocsElement(dElement.Element("returns")), //<value> will be transformed to <returns> by xslt in advance
                ThreadSafety = threadSafetyContent,
                ThreadSafetyInfo = threadSafety,
                Since = NormalizeDocsElement(dElement.Element("since")?.Value),
                AltCompliant = dElement.Element("altCompliant")?.Attribute("cref")?.Value,
                InternalOnly = dElement.Element("forInternalUseOnly") != null,
                Inheritdoc = inheritDoc
            };
        }

        /// <summary>
        /// merge altmember and seealso to seeAlso of yml file.
        /// </summary>
        /// <param name="dElement">a Xlement</param>
        /// <returns>List<string></returns>
        private static List<string> MergeAltmemberAndSeealsoToAltMemberCommentsIds(XElement dElement)
        {
            return dElement.Elements("altmember").Select(alt => alt.Attribute("cref").Value)
                .Concat(dElement.Elements("seealso").Where(alt=> alt.Attribute("cref") != null).Select(alt => alt.Attribute("cref").Value))
                .Distinct()
                .ToList();
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

        private static readonly string[] markdownHeaders = new string[]
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
                switch (line[i])
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
            string headerPrefix = markdownHeaders[headerCount - 1];
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

                    if (string.IsNullOrEmpty(tag.Text))
                    {
                        tag.Text = tag.Uri;
                    }

                    if (string.IsNullOrEmpty(tag.OriginalText))
                    {
                        tag.OriginalText = tag.Uri;
                    }

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

        private TypedContent GetTypedContent(XElement ele, string filePath)
        {
            var cref = ele.Attribute("cref").Value;

            // Bug 211134: Ci should throw warning if exception cref is not prefixed with type (T:)
            if (cref.IndexOf(':') <= 0)
            {
                OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_CrefTypePrefixMissing, filePath, cref, filePath);
            }
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
            List<int> codeBlockIndexes = null;
            for (int i = startIndex; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    lines[i] = "";
                    continue;
                }
                //MD code syntax must start without any indents, we shouldn't count this.
                if (lines[i].StartsWith("```"))
                {
                    codeBlockIndexes = codeBlockIndexes ?? new List<int>();
                    codeBlockIndexes.Add(i);
                    continue;
                }
                minIndent = Math.Min(minIndent, lines[i].CountIndent());
            }
            for (int i = startIndex; i < lines.Length; i++)
            {
                lines[i] = lines[i].TrimIndent(minIndent);
            }
            if (codeBlockIndexes?.Count > 0)
            {
                for (int i = 0; i < codeBlockIndexes.Count / 2; i++)
                {
                    minIndent = int.MaxValue;
                    int startIdx = codeBlockIndexes[i * 2];
                    int endIdx = codeBlockIndexes[i * 2 + 1];
                    //the line just before the second ```, if it's empty line, we should delete it.
                    if (string.IsNullOrWhiteSpace(lines[endIdx - 1]))
                    {
                        lines[endIdx - 1] = null;
                    }
                    //the line just after the first ```, if it's empty line, we should delete it.
                    if (string.IsNullOrWhiteSpace(lines[startIdx + 1]))
                    {
                        lines[startIdx + 1] = null;
                    }
                    //the line just before the first ```, if it's NOT empty line and ends with a html tag, we should add an extra linebreak.
                    //this is because how markdig handles html block.
                    if (startIdx > 0 && !string.IsNullOrWhiteSpace(lines[startIdx - 1]) && lines[startIdx - 1].EndsWith(">"))
                    {
                        lines[startIdx] = "\n" + lines[startIdx];
                    }
                    //if the second ``` itself is in the same line with other html tags, add an extra line break, for example ```</p><p>
                    if (lines[endIdx].Length > 3 && lines[endIdx][3] == '<')
                    {
                        lines[endIdx] = lines[endIdx].Replace("```", "```\n");
                    }

                    for (int j = startIdx + 1; j < endIdx; j++)
                    {
                        if (!string.IsNullOrEmpty(lines[j]))
                        {
                            minIndent = Math.Min(minIndent, lines[j].CountIndent());
                            lines[j] = lines[j].Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
                        }
                    }
                    if (minIndent < int.MaxValue)
                    {
                        for (int j = startIdx + 1; j < endIdx; j++)
                        {
                            lines[j] = string.IsNullOrEmpty(lines[j]) ? lines[j] : lines[j].Substring(minIndent);
                        }
                    }
                }
            }
            formatDetected = true;
            return string.Join("\n", lines.Skip(startIndex).Where(l => l != null));
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
