using IntellisenseFileGen;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;


namespace UnitTest
{
    [TestClass]
    public class IntellisenseFileTests
    {
        [DataTestMethod]
        [DataRow(@"Sets a value indicating whether the members of the global object should be made available to the script engine. \[Not presently supported.\]"
                    , "Sets a value indicating whether the members of the global object should be made available to the script engine. [Not presently supported.]")]
        [DataRow("The filter string. The default is \"*.\\*\" (Watches all files.)"
                    , "The filter string. The default is \"*.*\" (Watches all files.)")]
        public void SpecialProcessText_Test(string inText, string expected)
        {
            SpecialProcessValidation(inText, expected);
        }

        [DataTestMethod]
        [DataRow("The fully qualified name (*class.method*) of the method to use as an entry point when converting"
                    , "The fully qualified name (class.method) of the method to use as an entry point when converting")]
        [DataRow("The fully qualified name *classmethod* of *test 122* the method to use as an entry point when converting"
                    , "The fully qualified name classmethod of test 122 the method to use as an entry point when converting")]
        [DataRow("The fully qualified name *class.method* of the method to use as an entry point when converting"
                    , "The fully qualified name class.method of the method to use as an entry point when converting")]
        [DataRow("by the user in the form *functionname-arguments-ILoffset*. A named breakpoint"
                    , "by the user in the form functionname-arguments-ILoffset. A named breakpoint")]
        [DataRow("such as \"*.*\", is changed by setting the"
                    , "such as \".\", is changed by setting the")]
        [DataRow("this is `Unix` testing", "this is Unix testing")]
        [DataRow("with the default format *dd-mmm-yy*. For A.D. dates"
                    , "with the default format dd-mmm-yy. For A.D. dates")]
        [DataRow(" flag allows the parsed string to contain an exponent that begins with the \"E\" or \"e\" character and that is followed by an optional positive or negative sign and an integer. In other words, it successfully parses strings in the form *nnn*E*xx*, *nnn*E+*xx*, and *nnn*E-*xx*. It does not allow a decimal separator or sign in the significand or mantissa; to allow these elements in the string to be parsed, use the "
                    , " flag allows the parsed string to contain an exponent that begins with the \"E\" or \"e\" character and that is followed by an optional positive or negative sign and an integer. In other words, it successfully parses strings in the form nnnExx, nnnE+xx, and nnnE-xx. It does not allow a decimal separator or sign in the significand or mantissa; to allow these elements in the string to be parsed, use the ")]
        [DataRow("*flag* allows the *parsed*", "flag allows the parsed")]
        public void SingleSytax_Pattern_Test(string inText, string expected)
        {
            PatternValidate(inText, expected, Constants.SingleSytax_Pattern);
        }

        [DataTestMethod]
        [DataRow("**Abort** button was pressed. This member is equivalent to the Visual Basic constant **test234**"
                , "Abort button was pressed. This member is equivalent to the Visual Basic constant test234")]
        [DataRow("The valid values are **C#**, **VB**, and **C++**."
                , "The valid values are C#, VB, and C++.")]
        [DataRow("The brush for the **ExpandAll/CollapseAll** button in the designer view."
                , "The brush for the ExpandAll/CollapseAll button in the designer view.")]
        [DataRow("**Note: This API is now obsolete.** The non-obsolete alternative is"
                , "Note: This API is now obsolete. The non-obsolete alternative is")]
        [DataRow("be found in the **Rights-GUID** row"
                , "be found in the Rights-GUID row")]
        public void DoubleSytax_Pattern_Test(string inText, string expected)
        {
            PatternValidate(inText, expected, Constants.DoubleSytax_Pattern);
        }

        [DataTestMethod]
        [DataRow(@"```csharp  
Boolean isAvailable = scheduleObject.RawSchedule[2, 15, 3];
```", "Boolean isAvailable = scheduleObject.RawSchedule[2, 15, 3];")]
        [DataRow(@"```csharp Boolean isAvailable = scheduleObject.RawSchedule[2, 15, 3];```"
        , "Boolean isAvailable = scheduleObject.RawSchedule[2, 15, 3];")]
        public void TripleSytax_Pattern1_Test(string inText, string expected)
        {
            PatternValidate(inText, expected, Constants.TripleSytax_Pattern1);
        }

        [DataTestMethod]
        [DataRow(@"```  
Boolean isAvailable = scheduleObject.RawSchedule[2, 15, 3];
```", "Boolean isAvailable = scheduleObject.RawSchedule[2, 15, 3];")]
        [DataRow(@"``` Boolean isAvailable = scheduleObject.RawSchedule[2, 15, 3];```"
        , "Boolean isAvailable = scheduleObject.RawSchedule[2, 15, 3];")]
        public void TripleSytax_Pattern2_Test(string inText, string expected)
        {
            PatternValidate(inText, expected, Constants.TripleSytax_Pattern2);
        }

        [DataTestMethod]
        [DataRow("Indicates whether the type name for the configuration property requires transformation when it is serialized for an earlier version of the [!INCLUDE[dnprdnshort](~/includes/dnprdnshort-md.md)]."
                , "[!INCLUDE[dnprdnshort](~/includes/dnprdnshort-md.md)],~/includes/dnprdnshort-md.md")]
        public void Include_Pattern1_Test(string inText, string expected)
        {
            var matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.Include_Pattern1, inText);

            string[] expectedList = expected.Split(',');
            if (matches != null && matches.Length > 0)
            {
                Assert.AreEqual<int>(expectedList.Length,matches.Length);
                for (int i = 0; i < expectedList.Length; i++)
                {
                    Assert.AreEqual<string>(matches[i], expectedList[i]);
                }
            }
        }

        [DataTestMethod]
        [DataRow("Short include !INCLUDE[linq_dataset] test", "!INCLUDE[linq_dataset],linq_dataset")]
        public void Include_Pattern2_Test(string inText, string expected)
        {
            var matches = RegexHelper.GetMatches_All_JustWantedOne(Constants.Include_Pattern2, inText);

            string[] expectedList = expected.Split(',');
            if (matches != null && matches.Length > 0)
            {
                Assert.AreEqual<int>(expectedList.Length, matches.Length);
                for (int i = 0; i < expectedList.Length; i++)
                {
                    Assert.AreEqual<string>(matches[i], expectedList[i]);
                }
            }
        }

        [DataTestMethod]
        [DataRow("The is a interface [ISymUnmanagedWriter Interface](~/docs/framework/unmanaged-api/diagnostics/isymunmanagedwriter-interface.md) test"
                , "The is a interface ISymUnmanagedWriter Interface test")]
        public void Link_Pattern_Test(string inText, string expected)
        {
            PatternValidate(inText, expected, Constants.Link_Pattern);
        }

        private static void SpecialProcessValidation(string inText, string expected)
        {
            XText text = new XText(inText);
            IntellisenseFileGenHelper.SpecialProcessText(text);

            Assert.AreEqual<string>(text.Value, expected);
        }

        private static void PatternValidate(string inText, string expected, Regex regex)
        {
            string updatedContent = inText;

            var matches = RegexHelper.GetMatches_All_JustWantedOne(regex, inText);
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
