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
        [DataRow(@"https://docs.microsoft.com/en-us/dotnet/api/system.object?test=vb&view=netcore-3.1"
                    , "https://docs.microsoft.com/en-us/dotnet/api/system.object?test=vb&view=netcore-3.1")]
        [DataRow(@"https://docs.microsoft.com/en-us/previous-versions/ms180941(v=vs.90)?redirectedfrom=MSDN"
                    , "https://docs.microsoft.com/en-us/previous-versions/ms180941(v=vs.90)")]
        [DataRow("https://docs.microsoft.com/en-us/dotnet/api/system.object?redirectedfrom=MSDN&view=netcore-3.1"
                    , "https://docs.microsoft.com/en-us/dotnet/api/system.object?view=netcore-3.1")]
        [DataRow("https://docs.microsoft.com/en-us/dotnet/api/system.object?test=vb&redirectedfrom=MSDN&view=netcore-3.1"
                    , "https://docs.microsoft.com/en-us/dotnet/api/system.object?test=vb&view=netcore-3.1")]
        public void RemoveRedirectFromPart_Test(string inText, string expected)
        {
            var newUrl = new UrlRepaireHelper().RemoveUnusePartFromRedirectUrl(inText);
            Assert.AreEqual(expected, newUrl);
        }
    }
}
