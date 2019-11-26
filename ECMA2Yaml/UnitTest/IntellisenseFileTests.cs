using ECMA2Yaml;
using IntellisenseFileGen;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace UnitTest
{
    [TestClass]
    public class IntellisenseFileTests
    {
        [TestMethod]
        public void SpecialProcessText_Test()
        {
            Dictionary<string, string> testStrDic = new Dictionary<string, string>();
            testStrDic.Add(@"Sets a value indicating whether the members of the global object should be made available to the script engine. \[Not presently supported.\]", "Sets a value indicating whether the members of the global object should be made available to the script engine. [Not presently supported.]");
            testStrDic.Add("The filter string. The default is \"*.\\*\" (Watches all files.)", "The filter string. The default is \"*.*\" (Watches all files.)");

            testStrDic.ToList().ForEach(p =>
            {
                SpecialProcessValidation(p.Key, p.Value);
            });
        }

        [TestMethod]
        public void SingleSytax_Pattern_Test()
        {
            Dictionary<string, string> testStrDic = new Dictionary<string, string>();
            testStrDic.Add("The fully qualified name (*class.method*) of the method to use as an entry point when converting"
                , "The fully qualified name (class.method) of the method to use as an entry point when converting");
            testStrDic.Add("The fully qualified name *classmethod* of *test 122* the method to use as an entry point when converting"
                , "The fully qualified name classmethod of test 122 the method to use as an entry point when converting");
            testStrDic.Add("The fully qualified name *class.method* of the method to use as an entry point when converting"
                , "The fully qualified name class.method of the method to use as an entry point when converting");
            testStrDic.Add("by the user in the form *functionname-arguments-ILoffset*. A named breakpoint"
                , "by the user in the form functionname-arguments-ILoffset. A named breakpoint");
            testStrDic.Add("such as \"*.*\", is changed by setting the"
                , "such as \".\", is changed by setting the");
            testStrDic.Add("this is `Unix` testing", "this is Unix testing");
            testStrDic.Add("with the default format *dd-mmm-yy*. For A.D. dates"
                , "with the default format dd-mmm-yy. For A.D. dates");
            testStrDic.Add(" flag allows the parsed string to contain an exponent that begins with the \"E\" or \"e\" character and that is followed by an optional positive or negative sign and an integer. In other words, it successfully parses strings in the form *nnn*E*xx*, *nnn*E+*xx*, and *nnn*E-*xx*. It does not allow a decimal separator or sign in the significand or mantissa; to allow these elements in the string to be parsed, use the "
                , " flag allows the parsed string to contain an exponent that begins with the \"E\" or \"e\" character and that is followed by an optional positive or negative sign and an integer. In other words, it successfully parses strings in the form nnnExx, nnnE+xx, and nnnE-xx. It does not allow a decimal separator or sign in the significand or mantissa; to allow these elements in the string to be parsed, use the ");
            testStrDic.Add("*flag* allows the *parsed*", "flag allows the parsed");

            testStrDic.ToList().ForEach(p =>
            {
                PatternValidate(p.Key, p.Value, Constants.SingleSytax_Pattern);
            });
        }

        [TestMethod]
        public void DoubleSytax_Pattern_Test()
        {
            Dictionary<string, string> testStrDic = new Dictionary<string, string>();
            testStrDic.Add("**Abort** button was pressed. This member is equivalent to the Visual Basic constant **test234**"
                , "Abort button was pressed. This member is equivalent to the Visual Basic constant test234");
            testStrDic.Add("The valid values are **C#**, **VB**, and **C++**."
                , "The valid values are C#, VB, and C++.");
            testStrDic.Add("The brush for the **ExpandAll/CollapseAll** button in the designer view."
                , "The brush for the ExpandAll/CollapseAll button in the designer view.");

            testStrDic.ToList().ForEach(p =>
            {
                PatternValidate(p.Key, p.Value, Constants.DoubleSytax_Pattern);
            });
        }

        [TestMethod]
        public void Include_Pattern1_Test()
        {
            List<string> testStrList = new List<string>();
            testStrList.AddRange(new string[] {
                "Indicates whether the type name for the configuration property requires transformation when it is serialized for an earlier version of the [!INCLUDE[dnprdnshort](~/includes/dnprdnshort-md.md)]."
                ,"[!INCLUDE[dnprdnshort](~/includes/dnprdnshort-md.md)]"
                ,"~/includes/dnprdnshort-md.md"});

            string[] testStrArr = testStrList.ToArray();
            for (int i = 0; i < testStrArr.Length; i += 3)
            {
                var matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.Include_Pattern1, testStrArr[i]);
                if (matches != null && matches.Length == 2)
                {
                    Assert.AreEqual<string>(matches[0], testStrArr[i + 1]);
                    Assert.AreEqual<string>(matches[1], testStrArr[i + 2]);
                }
            }
        }

        [TestMethod]
        public void Include_Pattern2_Test()
        {
            List<string> testStrList = new List<string>();
            testStrList.AddRange(new string[] {
                "Short include !INCLUDE[linq_dataset] test"
                ,"!INCLUDE[linq_dataset]"
                ,"linq_dataset"});

            string[] testStrArr = testStrList.ToArray();
            for (int i = 0; i < testStrArr.Length; i += 3)
            {
                var matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.Include_Pattern2, testStrArr[i]);
                if (matches != null && matches.Length == 2)
                {
                    Assert.AreEqual<string>(matches[0], testStrArr[i + 1]);
                    Assert.AreEqual<string>(matches[1], testStrArr[i + 2]);
                }
            }
        }

        [TestMethod]
        public void Link_Pattern_Test()
        {
            Dictionary<string, string> testStrDic = new Dictionary<string, string>();
            testStrDic.Add("The is a interface [ISymUnmanagedWriter Interface](~/docs/framework/unmanaged-api/diagnostics/isymunmanagedwriter-interface.md) test"
                , "The is a interface ISymUnmanagedWriter Interface test");

            testStrDic.ToList().ForEach(p =>
            {
                PatternValidate(p.Key, p.Value, Constants.Link_Pattern);
            });
        }

        private static void SpecialProcessValidation(string inText, string expected)
        {
            XText text = new XText(inText);
            IntellisenseFileGenHelper.SpecialProcessText(text);

            Assert.AreEqual<string>(text.Value, expected);
        }

        private static void PatternValidate(string inText, string expected, string pattern)
        {
            string updatedContent = inText;

            var matches = RegexHelper.GetMatches_All_JustWantedOne(pattern, inText);
            if (matches != null && matches.Length >= 2)
            {
                for (int i = 0; i < matches.Length; i += 2)
                {
                    updatedContent = updatedContent.Replace(matches[i], matches[i + 1]);
                }
            }

            Assert.AreEqual(expected, updatedContent);
        }
    }
}
