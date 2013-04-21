using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace HeskyScript.Tests
{

    [TestClass]
    public class DslTests
    {
        const string simplestRule = "when id is 3 add spacebuck";

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

        [TestMethod, Ignore]
        public void CompoundRules1()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1) };
            const string compoundRule = "when id is 3 and Mode is Charlie add spacebuck";
            var output = Run(events, compoundRule);
            output.SpaceBucks.Should().Be(1);
        }

        [TestMethod]
        public void GreaterThan()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(5,1) };
            const string compoundRule = "when id GreaterThan 3 add spacebuck";
            var output = Run(events, compoundRule);
            output.SpaceBucks.Should().Be(1);
        }

        static Output Run(IEnumerable<Event> events, string rule = simplestRule, Mode mode = Mode.Charlie, Variant variant = Variant.Foxtrot)
        {
            return new Engine(rule).Run(events);
        }
    }
}
