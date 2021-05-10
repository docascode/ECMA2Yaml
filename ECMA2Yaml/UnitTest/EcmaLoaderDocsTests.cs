using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ECMA2Yaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class EcmaLoaderDocsTests
    {
        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("    ")]
        [DataRow("  \r\n   \r\n     \r\n  ")]
        [DataRow("  \r\n   \r\n     \r\n")]
        [DataRow("  \n   \n  \n  ")]
        [DataRow("  \n  \n    \n")]
        public void FormatTextIntoParagraphs_EmptyContentIsNotParsed(string content)
        {
            Assert.AreSame(content, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_SingleLineIsWrapped()
        {
            var content = "This is a single line of text";
            var expected = $"<p>{ content }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_MultipleLinesAreWrapped()
        {
            var content = $"This is a single line of text{ Environment.NewLine } This is a second line{ Environment.NewLine} This is a third";
            var expected = $"<p>{ content.Replace(Environment.NewLine, string.Empty) }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_MultipleLinesSpacesAreNormalized()
        {
            var first = "This is the first line.";
            var second = "A second line.";
            var third = "This is the third line.";
            var content = $"{ first }{ Environment.NewLine }{ second }{ Environment.NewLine}{ third }";
            var expected = $"<p>{ first } { second } { third }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [DataTestMethod]
        [DataRow(" ")]
        [DataRow("    ")]
        [DataRow("\t")]
        [DataRow("\t ")]
        [DataRow("\t\t")]
        public void FormatTextIntoParagraphs_LineIndentsAreNormalized(string indent)
        {
            var first = "This is the first line.";
            var second = "A second line.";
            var third = "This is the third line.";
            var content = $"{ indent }{first}{ Environment.NewLine }{ indent }{ second }{ Environment.NewLine }{ third }";
            var expected = $"<p>{ first } { second } { third }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_MultipleParagraphsAreWrapped()
        {
            var first = $"This is the first line.{ Environment.NewLine } This is a second line.";
            var second = "A second paragraph.";
            var third = "This is the third paragraph.";
            var content = $"{ first }{ Environment.NewLine }{ Environment.NewLine }{ second }{ Environment.NewLine }{ Environment.NewLine }{ third }";
            var expected = $"<p>{ first.Replace(Environment.NewLine, string.Empty) }</p><p>{ second }</p><p>{ third }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_MultipleParagraphsWithManualParagraphsAreWrapped()
        {
            var first = $"<p>This is the first line.</p>";
            var second = "A second paragraph.";
            var third = "This is the third paragraph.";
            var content = $"{ first }{ second }{ Environment.NewLine }{ Environment.NewLine }{ third }";
            var expected = $"{ first }<p>{ second }</p><p>{ third }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_MultipleLinesSpacesWithMultipleParagraphsAreNormalized()
        {
            var first = $"This is the first line.{ Environment.NewLine }This is a second line.";
            var second = $"A second paragraph.{ Environment.NewLine }With a second line.";
            var third = "This is the third paragraph.";
            var content = $"{ first }{ Environment.NewLine }{ Environment.NewLine }{ second }{ Environment.NewLine }{ Environment.NewLine }{ third }";
            var expected = $"<p>{ first.Replace(Environment.NewLine, " ") }</p><p>{ second.Replace(Environment.NewLine, " ") }</p><p>{ third }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_ExistingTagsArePreserved()
        {
            var content = "This is a <code>single<code> line of text";
            var expected = $"<p>{ content }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_ExistingTagsArePreservedForMultipleParagraphs()
        {
            var first = $"This is the <c>first</c> line.";
            var second = "A <b><i>second</i> paragraph</b>.";
            var third = "<code>This is the third paragraph.</code>";
            var content = $"{ first }{ Environment.NewLine }{ Environment.NewLine }{ second }{ Environment.NewLine }{ Environment.NewLine }{ third }";
            var expected = $"<p>{ first }</p><p>{ second }</p><p>{ third }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_ExistingParagraphsAreRecognized()
        {
            var content = "<p>This is a single< line of text</p>";
            Assert.AreEqual(content, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_ExistingParagraphsAreRecognizedForMultipleLines()
        {
            var content = $"<p>This is the first line of text.{ Environment.NewLine } This is the second.</p>";
            var expected = $"{ content.Replace(Environment.NewLine, string.Empty) }";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_ExistingParagraphsAreRecognizedForMultipleParagraphs()
        {
            var first = "<p>This is the first paragraph.</p>";
            var second = "<p>A second paragraph.</p>";
            var third = "<p>This is the third paragraph.</p>";
            var content = $"{ first }{ Environment.NewLine }{ Environment.NewLine }{ second }{ Environment.NewLine }{ Environment.NewLine }{ third }";
            var expected = $"{ first }{ second }{ third }";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_PartialExistingParagraphsAreRecognized()
        {
            var content = "<p>This is a single line of text";
            var expected = $"{ content }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_PartialExistingParagraphsAreRecognizedForMultipleLines()
        {
            var content = $"This is the first line of text.{ Environment.NewLine } This is the second.</p>";
            var expected = $"<p>{ content.Replace(Environment.NewLine, string.Empty) }";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_PartialExistingParagraphsAreRecognizedForMultipleParagraphs()
        {
            var first = "<p>This is the first paragraph.";
            var second = "A second paragraph.";
            var third = "This is the third paragraph.</p>";
            var content = $"{ first }{ Environment.NewLine }{ Environment.NewLine }{ second }{ Environment.NewLine }{ Environment.NewLine }{ third }";
            var expected = $"{ first }</p><p>{ second }</p><p>{ third }";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_LeadingBlanksAndTrailingAreIgnoredForSingleLine()
        {
            var content = $"{ Environment.NewLine }{ Environment.NewLine }This is a single line of text{ Environment.NewLine }{ Environment.NewLine }";
            var expected = $"<p>{ content.Replace(Environment.NewLine, string.Empty) }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_LeadingAndTrailingBlanksAreIgnoredForMultipleLines()
        {
            var content = $"{ Environment.NewLine }{ Environment.NewLine }This is a single line of text{ Environment.NewLine } This is a second line{ Environment.NewLine} This is a third{ Environment.NewLine }{ Environment.NewLine }";
            var expected = $"<p>{ content.Replace(Environment.NewLine, string.Empty) }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_ExtraBlanksAreIgnoredBetwenMultipleParagraphs()
        {
            var first = $"This is the first line.{ Environment.NewLine } This is a second line.";
            var second = "A second paragraph.";
            var third = "This is the third paragraph.";
            var content = $"{ Environment.NewLine }{ Environment.NewLine }{ first }{ Environment.NewLine }{ Environment.NewLine }{ Environment.NewLine }{ Environment.NewLine }{ second }{ Environment.NewLine }{ Environment.NewLine }{ Environment.NewLine }{ Environment.NewLine }{ third }{ Environment.NewLine }{ Environment.NewLine }";
            var expected = $"<p>{ first.Replace(Environment.NewLine, string.Empty) }</p><p>{ second }</p><p>{ third }</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_ParagraphsArePreservedWhenUsedInconsistentlyWithOtherTags()
        {
            var content = @"
            <p>
              Red/Blue/Yellow/Purple can become all color you want.
            </p>
            <ul>
              <li>
                Orange = Red + Yellow
              </li>
              <li>
                Purple = Red + Blue
              </li>
            </ul>";

            var expected = "<p>Red/Blue/Yellow/Purple can become all color you want.</p><p><ul> <li> Orange = Red + Yellow </li> <li> Purple = Red + Blue </li> </ul></p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_EmbeddedParagraphsAreRecognized()
        {
            var content = "This is <p>a single</p> line of text";
            var expected = $"<p>This is</p><p>a single</p><p> line of text</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_EmbeddedParagraphsAreRecognizedForMultipleLines()
        {
            var content = $"This is the<p>first line</p> of text.{ Environment.NewLine } This <p>is the</p> second.";
            var expected = "<p>This is the</p><p>first line</p><p> of text. This</p><p>is the</p><p> second.</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void FormatTextIntoParagraphs_EmbeddedParagraphsAreRecognizedForMultipleParagraphs()
        {
            var first = "<p>This is the first paragraph.";
            var second = "A <p>second paragraph.</p>";
            var third = "This <p>is the third</p> paragraph.</p>";
            var content = $"{ first }{ Environment.NewLine }{ Environment.NewLine }{ second }{ Environment.NewLine }{ Environment.NewLine }{ third }";
            var expected = "<p>This is the first paragraph.</p><p>A</p><p>second paragraph.</p><p>This</p><p>is the third</p><p> paragraph.</p>";

            Assert.AreEqual(expected, ECMALoader.FormatTextIntoParagraphs(content));
        }

        [TestMethod]
        public void LoadDocs_RemarksWithSimpleXmlDocComment()
        {
            var comments = XElement.Parse(
            @"<Docs>
                <summary>Default constructor.</summary>
                <remarks>This is an example of the format that is used by some libraries.</remarks>
              </Docs>");

            var expected = "<p>This is an example of the format that is used by some libraries.</p>";

            var parsed = new ECMALoader(null).LoadDocs(comments, "<<dummy>>");
            Assert.AreEqual(expected, parsed.Remarks);
        }

        [TestMethod]
        public void LoadDocs_RemarksWithSingleParagraphInXmlDocComment()
        {
            var comments = XElement.Parse(
            @"<Docs>
                <summary>
                  Default constructor.
                </summary>
                <remarks>
                  This is an example of the format that is used by some libraries, where
                  the lines of the remarks are broken up to increase readability for other
                  developers, but should be rendered as one paragraph for MS Docs.
                </remarks>
              </Docs>");

            var expected = "<p>This is an example of the format that is used by some libraries, where the lines of the remarks are broken up to increase readability for other developers, but should be rendered as one paragraph for MS Docs.</p>";

            var parsed = new ECMALoader(null).LoadDocs(comments, "<<dummy>>");
            Assert.AreEqual(expected, parsed.Remarks);
        }

        [TestMethod]
        public void LoadDocs_RemarksWithMultipleParagraphsInXmlDocComment()
        {
            var comments = XElement.Parse(
            @"<Docs>
                <summary>
                  Constructor with one generic parameter.
                </summary>

                <param name=""ownType"">This parameter type defined by class.</param>

                <remarks>
                  This is an example of the format that is used by some libraries, where
                  the lines of the remarks are broken up to increase readability.

                  Because of the blank line that precedes it, this should be considered
                  a new paragraph. Intellisense renders this correctly in VS, the
                  MS Docs rendering should follow suit.
                </remarks>
              </Docs>");

            var expected = "<p>This is an example of the format that is used by some libraries, where the lines of the remarks are broken up to increase readability.</p>"
                + "<p>Because of the blank line that precedes it, this should be considered a new paragraph. Intellisense renders this correctly in VS, the MS Docs rendering should follow suit.</p>";

            var parsed = new ECMALoader(null).LoadDocs(comments, "<<dummy>>");
            Assert.AreEqual(expected, parsed.Remarks);
        }

        [TestMethod]
        public void LoadDocs_RemarksWithEmbeddedParagraphsInXmlDocComment()
        {
            var comments = XElement.Parse(
            @"<root>
                <summary>
                  Constructor with one generic parameter.
                </summary>

                <param name=""ownType"">This parameter type defined by class.</param>

                <remarks>
                  This is an example of the format that is used by some libraries, where
                  the lines of the remarks are broken up to increase readability.

                  <p>Because of the blank line that precedes it, this should be considered
                  a new paragraph.</p> Intellisense renders this correctly in VS, the
                  MS Docs rendering should follow suit.
                </remarks>
              </root>");

            var expected = "<p>This is an example of the format that is used by some libraries, where the lines of the remarks are broken up to increase readability.</p>"
                + "<p>Because of the blank line that precedes it, this should be considered a new paragraph.</p><p> Intellisense renders this correctly in VS, the MS Docs rendering should follow suit.</p>";

            var parsed = new ECMALoader(null).LoadDocs(comments, "<<dummy>>");
            Assert.AreEqual(expected, parsed.Remarks);
        }

        [TestMethod]
        public void LoadDocs_RemarksWithExamplesInXmlDocComment()
        {
            var comments = XElement.Parse(
            @"<Docs>
                <summary>
                  Constructor with one generic parameter.
                </summary>

                <param name=""ownType"">This parameter type defined by class.</param>

                <remarks>Retry policies instruct the Storage Client to retry failed requests.
                 By default, only some failures are retried. For example, connection failures and
                 throttling failures can be retried. Resource not found (404) or authentication
                 failures are not retried, because these are not likely to succeed on retry.
                 If not set, the Storage Client uses an exponential backoff retry policy, where the wait time gets
                 exponentially longer between requests, up to a total of around 30 seconds.
                 The default retry policy is recommended for most scenarios.

                 ## Examples
                   [!code-csharp[Retry_Policy_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/BlobUploadDownloadTest.cs#sample_RequestOptions_RetryPolicy ""Retry Policy Sample"")]
                </remarks>
              </Docs>");

            var expectedRemarks = "<p>Retry policies instruct the Storage Client to retry failed requests. By default, only some failures are retried. For example, "
                + "connection failures and throttling failures can be retried. Resource not found (404) or authentication failures are not retried, because these "
                + "are not likely to succeed on retry. If not set, the Storage Client uses an exponential backoff retry policy, where the wait time gets exponentially "
                + "longer between requests, up to a total of around 30 seconds. The default retry policy is recommended for most scenarios.</p>";

            var expectedExamples = "[!code-csharp[Retry_Policy_Sample](~/azure-storage-net/Test/ClassLibraryCommon/Blob/BlobUploadDownloadTest.cs#sample_RequestOptions_RetryPolicy \"Retry Policy Sample\")]";

            var parsed = new ECMALoader(null).LoadDocs(comments, "<<dummy>>");
            Assert.AreEqual(expectedRemarks, parsed.Remarks);
            Assert.AreEqual(expectedExamples, parsed.Examples);
        }

        [TestMethod]
        [DataRow("<format>")]
        [DataRow("<format language =\"markdown\">")]
        [DataRow("<format >")]
        public void LoadDocs_FormatWrappedTextIsIgnored(string openTag)
        {
            var content = @"This should be totally ignored.  It should
               `not be modified at all.`
               **All the lines should be preserved.**

               Even this one.";

            var comments = XElement.Parse(
            $@"<Docs>
                <summary>
                  Constructor with one generic parameter.
                </summary>

                <param name=""ownType"">This parameter type defined by class.</param>

                <remarks>
                  { openTag }
                    { content }
                  </format>
                </remarks>
              </Docs>");

            // Normalize the indent to match formatting.

            var expected = string.Join("\n", content.Split('\n').Select(line => line.Trim()));

            var parsed = new ECMALoader(null).LoadDocs(comments, "<<dummy>>");
            Assert.AreEqual(expected, parsed.Remarks);
        }
    }
}
