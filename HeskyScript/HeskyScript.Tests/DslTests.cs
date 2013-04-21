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
        const string sampleRule1 = "when id is 5 add cookie\r\nwhen id is 6 add 2 cookies\r\nwhen id is 7 add widget";
        const string sampleRule2 = "when mode is charlie and variant is foxtrot\r\n\twhen id is 5 add cookie\r\n\twhen id is 6 add 2 cookies\r\n\twhen id is 7 add widget";

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
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(5,1) };
            const string compoundRule = "when id GreaterThan 3 add spacebuck";
            var output = Run(events, compoundRule);
            output.SpaceBucks.Should().Be(1);
        }

        [TestMethod]
        public void Sample1()
        {
            IEnumerable<Event> events = new[] { new Event(5, 3), new Event(6, 1), new Event(7, 2) };
            var result = Run(events, sampleRule1);
            result.Cookies.Should().Be(5, "3 cookies for event number 5 plus 2 cookies for event 6 should be 5 cookies!");
            result.Widgets.Should().Be(2, "2 widgets for event number 7");
        }

        [TestMethod]
        public void lt()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1), new Event(5, 1) };
            const string compoundRule = "when id lt 4 add spacebuck";
            var output = Run(events, compoundRule);
            output.SpaceBucks.Should().Be(1);
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
            output.SpaceBucks.Should().Be(1);
        }

        static Output Run(IEnumerable<Event> events, string rule = simplestRule, Mode mode = Mode.Charlie, Variant variant = Variant.Foxtrot)
        {
            var input = new Input(mode, variant);
            return new Engine(rule).Run(events, input);
        }
    }
}
