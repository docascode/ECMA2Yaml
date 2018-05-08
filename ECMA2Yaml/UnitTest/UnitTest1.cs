using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monodoc.Ecma;
using ECMA2Yaml;
using System.Collections.Generic;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestEcmaDesc1()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            EcmaDesc desc = EcmaParser.Parse("T:System.Nullable<Microsoft.Azure.Batch.Protocol.Models.AllocationState>");
            Console.WriteLine(desc.ToCompleteTypeName());
        }

        [TestMethod]
        public void TestEcmaDesc2()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            EcmaDesc desc = EcmaParser.Parse("T:System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>");
            Console.WriteLine(desc.ToSpecId());
            Console.WriteLine(desc.ToSpecId(new List<string>() { "TKey", "TValue" }));
        }

        [TestMethod]
        public void TestEcmaDesc3()
        {
            EcmaUrlParser EcmaParser = new EcmaUrlParser();
            EcmaDesc desc = EcmaParser.Parse("T:System.Collections.Generic.HashSet<T>+Enumerator[]");
            Assert.AreEqual("System.Collections.Generic.HashSet`1.Enumerator[]", desc.ToSpecId(new List<string>() { "T" }));
        }
    }
}
