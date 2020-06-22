using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSDNUrlPatch;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;


namespace UnitTest
{
    [TestClass]
    public class MSDNUrlPatchTests
    {
        [DataTestMethod]
        [DataRow(@"https://docs.microsoft.com/en-us/dotnet/api/system.object?test=vb&view1=netcore-3.1"
                    , "https://docs.microsoft.com/en-us/dotnet/api/system.object?test=vb&view1=netcore-3.1")]
        [DataRow(@"https://docs.microsoft.com/en-us/previous-versions/ms180941(v=vs.90)?redirectedfrom=MSDN"
                    , "https://docs.microsoft.com/en-us/previous-versions/ms180941(v=vs.90)")]
        [DataRow("https://docs.microsoft.com/en-us/dotnet/api/system.object?redirectedfrom=MSDN&view1=netcore-3.1"
                    , "https://docs.microsoft.com/en-us/dotnet/api/system.object?view1=netcore-3.1")]
        [DataRow("https://docs.microsoft.com/en-us/dotnet/api/system.object?test=vb&redirectedfrom=MSDN&view1=netcore-3.1"
                    , "https://docs.microsoft.com/en-us/dotnet/api/system.object?test=vb&view1=netcore-3.1")]
        [DataRow("https://docs.microsoft.com/en-us/visualstudio/msbuild/aspnetcompiler-task?view1=vs-2015&redirectedfrom=MSDN"
                    , "https://docs.microsoft.com/en-us/visualstudio/msbuild/aspnetcompiler-task?view1=vs-2015")]
        [DataRow("https://docs1.microsoft.com/en-us/visualstudio/msbuild/aspnetcompiler-task?view1=vs-2015&redirectedfrom=MSDN"
                    , "https://docs1.microsoft.com/en-us/visualstudio/msbuild/aspnetcompiler-task?view1=vs-2015")]
        [DataRow(@"https://docs.microsoft.com/en-us/windows/win32/seccng/cng-token-binding-functions\(v=vs.85\).aspx"
                    , "https://docs.microsoft.com/en-us/windows/win32/seccng/cng-token-binding-functions")]
        public void RemoveRedirectFromPart_Test(string inText, string expected)
        {
            CommandLineOptions option = new CommandLineOptions();
            var newUrl = new UrlRepairHelper(option).RemoveUnusePartFromRedirectUrl(inText);
            Assert.AreEqual(expected, newUrl);
        }

        [DataTestMethod]
        [DataRow("https://msdn.microsoft.com/library/30d5cc6d-b11e-4ff8-a51c-c58020eb41f9", "NoNeed")]
        public void GetDocsUrl_Test(string inText, string expected)
        {
            CommandLineOptions option = new CommandLineOptions() { BaseUrl = "https://docs.microsoft.com/en-us" };
            var newUrl = new UrlRepairHelper(option).GetDocsUrl(inText);
            Assert.AreEqual(expected, newUrl);
        }
    }
}
