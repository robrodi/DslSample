﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                var e = new Event(0, 0);
                Assert.Fail("Should Throw");
            }
            catch (Exception)
            {
            }
        }
    }
}
