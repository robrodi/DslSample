using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DslSample.Tests
{
    [TestClass]
    public class ContractTests
    {
        [TestMethod]
        public void EventContract()
        {
            try
            {
                new Event(0, 0);
                Assert.Fail("Should Throw");
            }
// ReSharper disable EmptyGeneralCatchClause
            catch (Exception)
// ReSharper restore EmptyGeneralCatchClause
            {
            }
        }
    }
}
