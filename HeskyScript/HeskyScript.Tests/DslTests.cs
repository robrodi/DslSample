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
            var wrapper = new Engine.TestWrapper(new Engine(Mode.Alpha, Variant.Echo, simplestRule));
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
        public void First_Cookie()
        {
            IEnumerable<Event> events = new[] { new Event(3, 1) };

            var output = Run(events, "when id is 3 add cookie");
            output.Cookies.Should().Be(1);
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

        static Output Run(IEnumerable<Event> events, string rule = simplestRule, Mode mode = Mode.Charlie, Variant variant = Variant.Foxtrot)
        {
            return new Engine(mode, variant, rule).Run(events);
        }
    }
}
