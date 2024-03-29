﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

using FluentAssertions;

namespace DslSample.Tests
{
    [TestClass]
    public class DslTests : TestBase
    {
        [TestMethod]
        public void Compile1()
        {
            var wrapper = new Engine.TestWrapper(new Engine(simplestRule));
            var result = wrapper.Compile();
            result.Should().NotBeNull();
        }

        [TestMethod]
        public void First()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1) };

            var output = Run(events);
            output.SpaceBucks.Should().Be(1);
        }

        [TestMethod]
        public void Subtract()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1) };

            var output = Run(events, simplestRule.Replace("add", "subtract"));
            output.SpaceBucks.Should().Be(-1);
        }

        [TestMethod]
        public void First_NCookies()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1) };

            var output = Run(events, "when id is 3 add 3 spacebucks");
            output.SpaceBucks.Should().Be(3);
        }

        [TestMethod]
        public void First_Cookie()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1) };

            var output = Run(events, "when id is 3 add cookie");
            output.Cookies.Should().Be(1);
        }

        [TestMethod]
        public void First_Widget()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1) };

            var output = Run(events, "when id is 3 add widget");
            output.Widgets.Should().Be(1);
        }


        [TestMethod]
        public void FirstWithCount()
        {
            IEnumerable<Event> events = new[] { new Event(3, 2) };

            var output = Run(events);
            output.SpaceBucks.Should().Be(2);
        }

        [TestMethod]
        public void MultipleSame()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(3, 1) };

            var output = Run(events);
            output.SpaceBucks.Should().Be(2);
        }
        [TestMethod]
        public void Different()
        {
            IEnumerable<Event> events = new[] { new Event(4, 1), new Event(3, 1) };
            var output = Run(events);
            output.SpaceBucks.Should().Be(1);
        }

        [TestMethod]
        public void TwoRules1()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1) };

            var output = Run(events, simplestRule + Environment.NewLine + simplestRule);
            output.SpaceBucks.Should().Be(2);
        }

        [TestMethod]
        public void TwoRules2()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1) };

            var output = Run(events, simplestRule + Environment.NewLine + simplestRule.Replace("spacebuck", "Cookie"));
            output.SpaceBucks.Should().Be(1);
            output.Cookies.Should().Be(1);
        }

        [TestMethod]
        public void TwoRules3()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(5,1) };

            var output = Run(events, simplestRule + Environment.NewLine + simplestRule.Replace("spacebuck", "Cookie").Replace('3', '5'));
            output.SpaceBucks.Should().Be(1);
            output.Cookies.Should().Be(1);
        }

        [TestMethod]
        public void GreaterThan()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(5, 1) };
            const string compoundRule = "when id GreaterThan 3 add spacebuck";
            var output = Run(events, compoundRule);
            output.SpaceBucks.Should().Be(1);
        }

        [TestMethod]
        public void GreaterThanOrEqual()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(5, 1) };
            const string compoundRule = "when id GreaterThanOrEqual 3 add spacebuck";
            var output = Run(events, compoundRule);
            output.SpaceBucks.Should().Be(2);
        }

        

        [TestMethod]
        public void lt()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(5, 1), new Event(5, 1) };
            const string compoundRule = "when id lt 4 add spacebuck";
            var output = Run(events, compoundRule);
            output.SpaceBucks.Should().Be(1);
        }

        [TestMethod]
        public void lte()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(5, 1), new Event(4, 1) };
            const string compoundRule = "when id lte 4 add spacebuck";
            var output = Run(events, compoundRule);
            output.SpaceBucks.Should().Be(2);
        }

        [TestMethod]
        public void MultipleCriteria()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(4, 1), new Event(3, 2) };

            var output = Run(events, "when id is 3 and count is 1 add cookie");
            output.Cookies.Should().Be(1);
        }

        [TestMethod]
        public void NoCriteria()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1) };
            var output = Run(events, "when add spacebuck");
            output.SpaceBucks.Should().Be(1);
            
        }

        [TestMethod]
        public void NoCriteria_NoWhen()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1) };
            var output = Run(events, "add spacebuck");
            output.SpaceBucks.Should().Be(1);

        }

        [TestMethod]
        public void GlobalValue()
        {
            const string compoundRule = "when mode is Charlie add 1 spacebuck";
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(5, 1) };
            var output = Run(events, compoundRule);
            output.SpaceBucks.Should().Be(2);
        }

        [TestMethod]
        public void EventAndGlobalValue()
        {
            const string compoundRule = "when mode is Charlie and id is 3 add 1 spacebuck";
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(5, 1) };
            var output = Run(events, compoundRule);
            output.SpaceBucks.Should().Be(1);
        }

        [TestMethod]
        public void NotEqual()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event (4,2) };

            var output = Run(events, simplestRule.Replace("is", "neq"));
            output.SpaceBucks.Should().Be(2);
        }
    }
}
