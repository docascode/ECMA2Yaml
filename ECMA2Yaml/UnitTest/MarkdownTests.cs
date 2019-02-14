using ECMA2Yaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    [TestClass]
    public class MarkdownTests
    {
        [TestMethod]
        public void DowngradeMarkdownHeader_H1_Change() => AssertChange("# Thing\n## Thing", $"## Thing{Environment.NewLine}### Thing", assert: Assert.AreEqual);
        [TestMethod]
        public void DowngradeMarkdownHeader_H1_NoChange() => AssertChange("# Thing", "# Thing", assert: Assert.AreEqual);
        [TestMethod]
        public void DowngradeMarkdownHeader_H2() => AssertChange("## Thing", "### Thing");
        [TestMethod]
        public void DowngradeMarkdownHeader_H3() => AssertChange("### Thing", "### Thing", assert: Assert.AreEqual);
        [TestMethod]
        public void DowngradeMarkdownHeader_H3_Change() => AssertChange("## Thing\n### Thing", $"### Thing{Environment.NewLine}#### Thing", assert: Assert.AreEqual);
        [TestMethod]
        public void DowngradeMarkdownHeader_H4() => AssertChange("#### Thing", "#### Thing", assert: Assert.AreEqual);
        [TestMethod]
        public void DowngradeMarkdownHeader_H4_Change() => AssertChange("## Thing\n#### Thing", $"### Thing{Environment.NewLine}##### Thing", assert: Assert.AreEqual);
        [TestMethod]
        public void DowngradeMarkdownHeader_H5() => AssertChange("##### Thing", "##### Thing", assert: Assert.AreEqual);
        [TestMethod]
        public void DowngradeMarkdownHeader_H5_Change() => AssertChange("## Thing\n##### Thing", $"### Thing{Environment.NewLine}###### Thing", assert: Assert.AreEqual);
        [TestMethod]
        public void DowngradeMarkdownHeader_H6() => AssertChange("###### Thing", "###### Thing", assert: Assert.AreEqual); // no change

        // mixed headers

        [TestMethod]
        public void DowngradeMarkdownHeader_MixedHeaders() => AssertChange("# Thing\n## Thing\n### Thing\n#### Thing\n##### Thing\n###### Thing\n", $"## Thing{Environment.NewLine}### Thing{Environment.NewLine}#### Thing{Environment.NewLine}##### Thing{Environment.NewLine}###### Thing{Environment.NewLine}###### Thing{Environment.NewLine}");

        // mixed line endings

        [TestMethod]
        public void DowngradeMarkdownHeader_MixedLineEnds() => AssertChange("# Thing\n## Thing\r\n### Thing\r\n\n", $"## Thing{Environment.NewLine}### Thing{Environment.NewLine}#### Thing{Environment.NewLine}{Environment.NewLine}");


        // code that contains hashes
        [TestMethod]
        public void DowngradeMarkdownHeader_MixedHashContent() => AssertChange("# Thing\n```\nSome ##Code \n# A comment\n```\nafter code block", $"# Thing{Environment.NewLine}```{Environment.NewLine}Some ##Code {Environment.NewLine}# A comment{Environment.NewLine}```{Environment.NewLine}after code block");
        [TestMethod]
        public void DowngradeMarkdownHeader_MixedHashContent_Withh2() => AssertChange("# Thing\n```\nSome ##Code \n# A comment\n```\n##after code block", $"## Thing{Environment.NewLine}```{Environment.NewLine}Some ##Code {Environment.NewLine}# A comment{Environment.NewLine}```{Environment.NewLine}###after code block");


        // headers with 3 or fewer spaces in front
        [TestMethod]
        public void DowngradeMarkdownHeader_Whitespace() => AssertChange(" # Thing\n```\nSome ##Code \n# A comment\n```\n   ## after code block", $" ## Thing{Environment.NewLine}```{Environment.NewLine}Some ##Code {Environment.NewLine}# A comment{Environment.NewLine}```{Environment.NewLine}   ### after code block");


        private static void AssertChange(string startText, string expected, Action<string,string> assert=null)
        {
            var actual = ECMALoader.DowngradeMarkdownHeaders(startText);
            
            if (assert == null)
                Assert.AreEqual(expected, actual);
            else
                assert(expected, actual);
        }
    }
}
