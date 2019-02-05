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
        public void DowngradeMarkdownHeader_H1() => AssertChange("# Thing", "## Thing");
        [TestMethod]
        public void DowngradeMarkdownHeader_H2() => AssertChange("## Thing", "### Thing");
        [TestMethod]
        public void DowngradeMarkdownHeader_H3() => AssertChange("### Thing", "#### Thing");
        [TestMethod]
        public void DowngradeMarkdownHeader_H4() => AssertChange("#### Thing", "##### Thing");
        [TestMethod]
        public void DowngradeMarkdownHeader_H5() => AssertChange("##### Thing", "###### Thing");
        [TestMethod]
        public void DowngradeMarkdownHeader_H6() => AssertChange("###### Thing", "###### Thing", shouldBeSame:true); // no change

        // mixed headers

        [TestMethod]
        public void DowngradeMarkdownHeader_MixedHeaders() => AssertChange("# Thing\n## Thing\n### Thing\n#### Thing\n##### Thing\n###### Thing\n", $"## Thing{Environment.NewLine}### Thing{Environment.NewLine}#### Thing{Environment.NewLine}##### Thing{Environment.NewLine}###### Thing{Environment.NewLine}###### Thing{Environment.NewLine}");

        // mixed line endings

        [TestMethod]
        public void DowngradeMarkdownHeader_MixedLineEnds() => AssertChange("# Thing\n## Thing\r\n### Thing\r\n\n", $"## Thing{Environment.NewLine}### Thing{Environment.NewLine}#### Thing{Environment.NewLine}{Environment.NewLine}");


        // code that contains hashes
        [TestMethod]
        public void DowngradeMarkdownHeader_MixedHashContent() => AssertChange("# Thing\n```\nSome ##Code \n# A comment```\nafter code block", $"## Thing{Environment.NewLine}```{Environment.NewLine}Some ##Code {Environment.NewLine}# A comment```{Environment.NewLine}after code block");

        private static void AssertChange(string startText, string expected, bool shouldBeSame = false)
        {
            var actual = ECMALoader.DowngradeMarkdownHeaders(startText);

            if (!shouldBeSame)
                Assert.AreNotEqual(actual, startText);
            Assert.AreEqual(expected, actual);
        }
    }
}
