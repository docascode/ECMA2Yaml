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
            string testDirectory = Path.GetFullPath("..\\..\\..\\..\\test");
            string xmlDirectory = Path.Combine(testDirectory, "xml");
            string outputDirectory = Path.Combine(testDirectory, "_yml_UnitTests_ECMA2YamlConverter_HappyPath");
            string sourceMapFilePath = Path.Combine(testDirectory, "_yml_UnitTests_ECMA2YamlConverter_HappyPath\\.sourcemap.json");
            ECMA2YamlConverter.Run(
                xmlDirectory,
                outputDirectory,
                logWriter: item => Console.WriteLine(item.ToString()),
                sourceMapFilePath: sourceMapFilePath);
        }
    }
}
