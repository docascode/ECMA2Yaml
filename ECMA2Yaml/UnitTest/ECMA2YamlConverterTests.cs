using System;
using System.IO;
using ECMA2Yaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class ECMA2YamlConverterTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ECMA2YamlConverter_HappyPath()
        {
            string repoPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(TestContext.TestDir)));
            string xmlDirectory = Path.Combine(repoPath, "test\\xml");
            string outputDirectory = Path.Combine(repoPath, "test\\_yml_UnitTests_ECMA2YamlConverter_HappyPath");
            string fallbackXmlDirectory = null;
            string logFilePath = null;
            string logContentBaseDirectory = null;
            string sourceMapFilePath = null;
            ECMA2YamlConverter.Run(xmlDirectory, outputDirectory);
        }
    }
}
