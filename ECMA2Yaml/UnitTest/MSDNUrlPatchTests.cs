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
        [DataRow(@"https://msdn.microsoft.com/library/e5ae402f-6dda-4732-bbe8-77296630f675"
                    , "/previous-versions/h846e9b3(v=vs.110)",true)]
        [DataRow(@"https://msdn.microsoft.com/library/e5ae402f-6dda-4732-bbe8-77296630f675"
                    , "NoNeed", false)]
        public void PreVersions_Switch_Test(string inText, string expected, bool isPreVersions)
        {
            CommandLineOptions option = new CommandLineOptions() { BaseUrl = "https://docs.microsoft.com/en-us", PreVersions=isPreVersions };
            var newUrl = new UrlRepairHelper(option).GetDocsUrl(inText);
            Assert.AreEqual(expected, newUrl);
        }

        [DataTestMethod]
        [DataRow(@"https://msdn.microsoft.com/library/f811c019-a67b-4d54-82e6-e29549496f6e"
                    , "/visualstudio/msbuild/aspnetcompiler-task?view=vs-2015", true)]
        [DataRow(@"https://msdn.microsoft.com/library/f811c019-a67b-4d54-82e6-e29549496f6e"
                    , "NoNeed", false)]
        public void FixedVersions_Switch_Test(string inText, string expected, bool isFixedVersions)
        {
            CommandLineOptions option = new CommandLineOptions() { BaseUrl = "https://docs.microsoft.com/en-us",  FixedVersions = isFixedVersions };
            var newUrl = new UrlRepairHelper(option).GetDocsUrl(inText);
            Assert.AreEqual(expected, newUrl);
        }

        [DataTestMethod]
        [DataRow("https://msdn.microsoft.com", "https://docs.microsoft.com")]
        public void GetDocsUrl_Test(string inText, string expected)
        {
            CommandLineOptions option = new CommandLineOptions() { BaseUrl = "https://docs.microsoft.com/en-us" };
            var newUrl = new UrlRepairHelper(option).GetDocsUrl(inText);
            Assert.AreEqual(expected, newUrl);
        }

        [DataTestMethod]
        [DataRow("ms:mtpsurl: https://msdn.microsoft.com/library/microsoft.web.management.databasemanager.column.allownulls"
            , "ms:mtpsurl: https://msdn.microsoft.com/library/microsoft.web.management.databasemanager.column.allownulls")]
        [DataRow("mstest:mtpsurl: https://msdn.microsoft.com/library/microsoft.web.management.databasemanager.column.allownulls"
            , "mstest:mtpsurl: /iis/extensions/database-manager-reference/column-allownulls-property-microsoft-web-management-databasemanager")]
        [DataRow("This is a page [test](https://msdn.microsoft.com/library/0eee8ced-ad68-427d-b95a-97260e98deed) for test"
            , "This is a page <https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/file-schema/wcf/wshttpbinding> for test")]
        [DataRow("the Forms Authentication module (see docs at [https://msdn.microsoft.com/library/1d3t3c61.aspx](https://msdn.microsoft.com/library/1d3t3c61.aspx))"
            , "the Forms Authentication module (see docs at <https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/1d3t3c61(v=vs.100)>)")]
        [DataRow("see [Code First Data Annotations](https://msdn.microsoft.com/library/jj591583(v=vs.113).aspx)"
               , "see <https://docs.microsoft.com/en-us/ef/ef6/modeling/code-first/data-annotations>")]
        public void RepairString_Test(string inText, string expected)
        {
            CommandLineOptions option = new CommandLineOptions() { BaseUrl = "https://docs.microsoft.com/en-us" , FixedVersions= true, PreVersions=true };
            var newText = new UrlRepairHelper(option).RepairString(inText, null);
            Assert.AreEqual(expected, newText);
        }
    }
}
