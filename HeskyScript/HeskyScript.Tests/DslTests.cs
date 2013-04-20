﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace HeskyScript.Tests
{

    [TestClass]
    public class DslTests
    {
        
        [TestMethod]
        public void First()
        {
            const string rule = "when mode is charlie\r\n\twhen id is 3 add spacebuck";
            IEnumerable<Event> events = new[] { new Event(3, 1) };

            var output = new Engine().Run(events, rule);
            output.SpaceBucks.Should().Be(1);
        }
        [TestMethod]
        public void MultipleSame()
        {
            const string rule = "when mode is charlie\r\n\twhen id is 3 add spacebuck";
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(3, 1) };

            var output = new Engine().Run(events, rule);
            output.SpaceBucks.Should().Be(2);
        }
        [TestMethod]
        public void Different()
        {
            const string rule = "when mode is charlie\r\n\twhen id is 3 add spacebuck";
            IEnumerable<Event> events = new[] { new Event(4, 1), new Event(3, 1) };

            var output = new Engine().Run(events, rule);
            output.SpaceBucks.Should().Be(1);
        }
    }
}