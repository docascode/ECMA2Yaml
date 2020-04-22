using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECMA2Yaml;
using ECMA2Yaml.IO;

namespace UnitTest
{
    [TestClass]
    public class ExtensionTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            // reset the separator char back to default between tests
            StringExtensions.DirectorySeparatorChar = System.IO.Path.DirectorySeparatorChar;
        }

        [TestMethod]
        public void Test_UnixOnUnix()
        {
            StringExtensions.DirectorySeparatorChar = '/';
            string path = "a/unix/path/on/unix";

            // should be unchanged
            Assert.AreEqual(path, path.NormalizePath());
        }

        [TestMethod]
        public void Test_UnixOnWin()
        {
            StringExtensions.DirectorySeparatorChar = '\\';
            string path = "a/unix/path/on/win";

            // Separators should be flipped
            Assert.AreEqual(@"a\unix\path\on\win", path.NormalizePath());
        }

        [TestMethod]
        public void Test_WinOnWin()
        {
            StringExtensions.DirectorySeparatorChar = '\\';
            string path = @"a\win\path\on\win";

            // should be unchanged
            Assert.AreEqual(path, path.NormalizePath());
        }

        [TestMethod]
        public void Test_WinOnUnix()
        {
            StringExtensions.DirectorySeparatorChar = '/';
            string path = @"a\win\path\on\unix";

            // Separators should be flipped
            Assert.AreEqual(@"a/win/path/on/unix", path.NormalizePath());
        }

        [TestMethod]
        public void Test_MixedOnUnix()
        {
            StringExtensions.DirectorySeparatorChar = '/';
            string path = @"a\mixed/path\on/unix";

            // Separators should be consistent
            Assert.AreEqual(@"a/mixed/path/on/unix", path.NormalizePath());
        }

        [TestMethod]
        public void Test_MixedOnWin()
        {
            StringExtensions.DirectorySeparatorChar = '\\';
            string path = @"a\mixed/path\on/win";

            // Separators should be consistent
            Assert.AreEqual(@"a\mixed\path\on\win", path.NormalizePath());
        }

        [TestMethod]
        public void Test_NoNullReferenceException()
        {
            string aNullString = null;

            // this should not throw an exception
            Assert.IsNull(aNullString.NormalizePath());
        }

        [DataTestMethod]
        [DataRow("Bar", "Foo", "..\\Bar")]
        [DataRow("C:\\Bar", "C:\\Foo", "..\\Bar")]
        [DataRow("C:\\Foo\\Bar", "C:\\Foo", "Bar")]
        [DataRow("C:\\Bar\\Bar", "C:\\Foo\\Bar", "..\\..\\Bar\\Bar")]
        [DataRow("Bar.txt", "Foo", "..\\Bar.txt")]
        [DataRow("Bar", "Foo.txt", "..\\Bar")]
        public void FileAbstractLayer_RelativePath_Test(string path, string relativeTo, string expected)
        {
            var result = FileAbstractLayer.RelativePath(path, relativeTo);
            Assert.AreEqual<string>(expected, result);
        }
    }
}
