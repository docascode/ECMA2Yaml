using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monodoc.Ecma;
using ECMA2Yaml;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace UnitTest
{
    [TestClass]
    public class EcmaDescTest
    {
        [TestMethod]
        public void TestEcmaDesc1()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:System.Nullable<Microsoft.Azure.Batch.Protocol.Models.AllocationState>");
            Console.WriteLine(desc.ToCompleteTypeName());
        }

        [TestMethod]
        public void TestEcmaDesc2()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>");
            Console.WriteLine(desc.ToSpecId());
            Console.WriteLine(desc.ToSpecId(new List<string>() { "TKey", "TValue" }));
        }

        [TestMethod]
        public void TestEcmaDesc3()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:System.Collections.Generic.HashSet<T>+Enumerator[]");
            Assert.AreEqual("System.Collections.Generic.HashSet{`0}.Enumerator[]", desc.ToSpecId(new List<string>() { "T" }));
        }

        [TestMethod]
        public void TestEcmaDescToMD_Generic()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:System.Collections.Generic.Dictionary<Systen.String, Syste.IO.File>");
            var md = SDPYamlConverter.DescToTypeMDString(desc);
            var expected = "<xref href=\"System.Collections.Generic.Dictionary`2?alt=System.Collections.Generic.Dictionary&text=Dictionary\" data-throw-if-not-resolved=\"True\"/>&lt;<xref href=\"Systen.String?alt=Systen.String&text=String\" data-throw-if-not-resolved=\"True\"/>,<xref href=\"Syste.IO.File?alt=Syste.IO.File&text=File\" data-throw-if-not-resolved=\"True\"/>&gt;";
            Assert.AreEqual(expected, md);
        }

        [TestMethod]
        public void TestEcmaDescToMD_NestedGeneric()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:Namespace.Class+NestedClass<Systen.String, Syste.IO.File>");
            var md = SDPYamlConverter.DescToTypeMDString(desc);
            var expected = "<xref href=\"Namespace.Class.NestedClass`2?alt=Class.NestedClass&text=Class.NestedClass\" data-throw-if-not-resolved=\"True\"/>&lt;<xref href=\"Systen.String?alt=Systen.String&text=String\" data-throw-if-not-resolved=\"True\"/>,<xref href=\"Syste.IO.File?alt=Syste.IO.File&text=File\" data-throw-if-not-resolved=\"True\"/>&gt;";
            Assert.AreEqual(expected, md);
        }

        [TestMethod]
        public void TestEcmaDescToMD_NestedNestedNested()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:Namespace.Class+NestedClass+NestedNestedClass");
            var md = SDPYamlConverter.DescToTypeMDString(desc);
            var expected = "<xref href=\"Namespace.Class.NestedClass.NestedNestedClass?alt=Namespace.Class.NestedClass.NestedNestedClass&text=Class.NestedClass.NestedNestedClass\" data-throw-if-not-resolved=\"True\"/>";
            Assert.AreEqual(expected, md);
        }

        [TestMethod]
        public void TestEcmaDescToMD_NestedGenericNested()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:Microsoft.ML.StaticPipe.TermStaticExtensions+ToKeyFitResult<System.Boolean>+OnFit");
            var md = SDPYamlConverter.DescToTypeMDString(desc);
            var expected = "<xref href=\"Microsoft.ML.StaticPipe.TermStaticExtensions.ToKeyFitResult`1?alt=TermStaticExtensions.ToKeyFitResult&text=TermStaticExtensions.ToKeyFitResult\" data-throw-if-not-resolved=\"True\"/>&lt;<xref href=\"System.Boolean?alt=System.Boolean&text=Boolean\" data-throw-if-not-resolved=\"True\"/>&gt;.<xref href=\"Microsoft.ML.StaticPipe.TermStaticExtensions.ToKeyFitResult`1.OnFit?alt=Microsoft.ML.StaticPipe.TermStaticExtensions.ToKeyFitResult`1.OnFit&text=OnFit\" data-throw-if-not-resolved=\"True\"/>";
            Assert.AreEqual(expected, md);
        }

        [TestMethod]
        public void TestEcmaDescToMD_NestedGenericGenericNested()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:Microsoft.ML.StaticPipe.TermStaticExtensions+ToKeyFitResult<System.ReadOnlyMemory<System.Char>>+OnFit");
            var md = SDPYamlConverter.DescToTypeMDString(desc);
            var expected = "<xref href=\"Microsoft.ML.StaticPipe.TermStaticExtensions.ToKeyFitResult`1?alt=TermStaticExtensions.ToKeyFitResult&text=TermStaticExtensions.ToKeyFitResult\" data-throw-if-not-resolved=\"True\"/>&lt;<xref href=\"System.ReadOnlyMemory`1?alt=System.ReadOnlyMemory&text=ReadOnlyMemory\" data-throw-if-not-resolved=\"True\"/>&lt;<xref href=\"System.Char?alt=System.Char&text=Char\" data-throw-if-not-resolved=\"True\"/>&gt;&gt;.<xref href=\"Microsoft.ML.StaticPipe.TermStaticExtensions.ToKeyFitResult`1.OnFit?alt=Microsoft.ML.StaticPipe.TermStaticExtensions.ToKeyFitResult`1.OnFit&text=OnFit\" data-throw-if-not-resolved=\"True\"/>";
            Assert.AreEqual(expected, md);
        }

        [TestMethod]
        public void TestEcmaDescToMD_GenericNested()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:System.Collections.Generic.HashSet<Namespace.Class+NestedClass>");
            var md = SDPYamlConverter.DescToTypeMDString(desc);
            var expected = "<xref href=\"System.Collections.Generic.HashSet`1?alt=System.Collections.Generic.HashSet&text=HashSet\" data-throw-if-not-resolved=\"True\"/>&lt;<xref href=\"Namespace.Class.NestedClass?alt=Namespace.Class.NestedClass&text=Class.NestedClass\" data-throw-if-not-resolved=\"True\"/>&gt;";
            Assert.AreEqual(expected, md);
        }

        [TestMethod]
        public void TestEcmaDescToMD_GenericNestedArray()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:System.Collections.Generic.HashSet<T>+Enumerator[]");
            var md = SDPYamlConverter.DescToTypeMDString(desc);
            var expected = "<xref href=\"System.Collections.Generic.HashSet`1?alt=System.Collections.Generic.HashSet&text=HashSet\" data-throw-if-not-resolved=\"True\"/>&lt;T&gt;.<xref href=\"System.Collections.Generic.HashSet`1.Enumerator?alt=System.Collections.Generic.HashSet`1.Enumerator&text=Enumerator\" data-throw-if-not-resolved=\"True\"/>[]";
            Assert.AreEqual(expected, md);
        }

        [TestMethod]
        public void TestEcmaDescToMD_GenericArray()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:System.Predicate<T[]>");
            var md = SDPYamlConverter.DescToTypeMDString(desc);
            var expected = "<xref href=\"System.Predicate`1?alt=System.Predicate&text=Predicate\" data-throw-if-not-resolved=\"True\"/>&lt;T[]&gt;";
            Assert.AreEqual(expected, md);
        }

        [TestMethod, Ignore]
        // prepare test case for bug 102828
        public void TestEcmaDesc_Matrix()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:System.Management.Automation.Host.BufferCell[,]");
            var md = SDPYamlConverter.DescToTypeMDString(desc);
            var expected = "<xref href=\"System.Predicate`1?alt=System.Predicate&text=Predicate\" data-throw-if-not-resolved=\"True\"/>&lt;T[,]&gt;";
            Assert.AreEqual(expected, md);

            desc = EcmaParser.Parse("T:System.Management.Automation.Host.BufferCell[,,]");
            md = SDPYamlConverter.DescToTypeMDString(desc);
            expected = "<xref href=\"System.Predicate`1?alt=System.Predicate&text=Predicate\" data-throw-if-not-resolved=\"True\"/>&lt;T[,,]&gt;";
            Assert.AreEqual(expected, md);
        }

        [TestMethod, Ignore]
        public void TestEcmaDesc_Complex()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<Microsoft.Bot.Builder.Scorables.Internals.FoldScorable<Item,Score>.State>>");
        }

        [TestMethod, Ignore]
        public void TestXmlIndent()
        {
            ECMALoader loader = new ECMALoader(null);
            XElement element = XElement.Load(@"e:\mdoc\docs.xml");
            var docs = loader.LoadDocs(element);
        }
    }
}
