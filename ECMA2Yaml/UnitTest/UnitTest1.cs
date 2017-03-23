using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monodoc.Ecma;

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
    }
}
