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
        public void RemoveRedirectFromPart_Test(string inText, string expected)
        {
            var newUrl = new UrlRepairHelper().RemoveUnusePartFromRedirectUrl(inText);
            Assert.AreEqual(expected, newUrl);
        }
    }
}
