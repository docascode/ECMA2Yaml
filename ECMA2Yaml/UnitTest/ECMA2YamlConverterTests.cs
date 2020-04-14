﻿using System;
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
            string xmlDirectory = Path.GetFullPath("..\\..\\..\\..\\test\\xml");
            string outputDirectory = Path.GetFullPath("..\\..\\..\\..\\test\\_yml_UnitTests_ECMA2YamlConverter_HappyPath");
            string fallbackXmlDirectory = null;
            string logFilePath = null;
            string logContentBaseDirectory = null;
            string sourceMapFilePath = null;
            ECMA2YamlConverter.Run(xmlDirectory, outputDirectory);
        }
    }
}