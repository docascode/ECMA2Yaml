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
        public void TestEcmaDescToMD_Complex()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:System.Collections.Generic.HashSet<T>+Enumerator[]");
            var md = SDPYamlConverter.DescToTypeMDString(desc);
            Assert.AreEqual("<xref:System.Collections.Generic.HashSet`1?alt=System.Collections.Generic.HashSet`1&text=HashSet>&lt;T&gt;.<xref:System.Collections.Generic.HashSet`1.Enumerator?alt=System.Collections.Generic.HashSet`1.Enumerator&text=Enumerator>[]", md);
        }

        [TestMethod]
        public void TestEcmaDescToMD_GenericArray()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            Monodoc.Ecma.EcmaDesc desc = EcmaParser.Parse("T:System.Predicate<T[]>");
            var md = SDPYamlConverter.DescToTypeMDString(desc);
            Assert.AreEqual("<xref:System.Predicate`1?alt=System.Predicate`1&text=Predicate>&lt;T[]&gt;", md);
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
