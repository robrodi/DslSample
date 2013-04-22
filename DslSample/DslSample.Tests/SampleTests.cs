using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace DslSample.Tests
{
    [TestClass]
    public class SampleTests : TestBase
    {
        const string sampleRule1 = "when id is 5 add cookie\r\nwhen id is 6 add 2 cookies\r\nwhen id is 7 add widget";
        const string sampleRule2 =
@"when id is 5 and count GreaterThan 4 add cookie
when id is 6 and count is 3 add 2 cookies";

        const string sampleRule3 =
            @"when mode is Alpha and count GreaterThan 4 add cookie
when Variant is foxtrot and count is 3 add 2 cookies";

        const string sampleRule4 =
@"when id is 5 add cookie
# I'm a comment
when id is 7 add widget

# I'm a comment preceded by a newline!
when id is 6 add 2 cookies

// I'm a comment too.
when id is 9 add 2 cookies  ";
        const string sampleRule5 = "when mode is charlie and variant is foxtrot\r\n\twhen id is 5 add cookie\r\n\twhen id is 6 add 2 cookies\r\n\twhen id is 7 add widget";

        [TestMethod]
        public void Sample1()
        {
            IEnumerable<Event> events = new[] { new Event(5, 3), new Event(6, 1), new Event(7, 2) };
            var result = Run(events, sampleRule1);
            result.Cookies.Should().Be(5, "3 cookies for event number 5 plus 2 cookies for event 6 should be 5 cookies!");
            result.Widgets.Should().Be(2, "2 widgets for event number 7");
        }

        [TestMethod]
        public void Sample2()
        {
            IEnumerable<Event> events = new[] { new Event(5, 5), new Event(5, 1), new Event(6, 3), new Event(7, 2) };
            var result = Run(events, sampleRule2);
            result.Cookies.Should().Be(7, "5 cookies for event number 5 plus 2 cookies for event 6 should be 7 cookies!");
            result.Widgets.Should().Be(0);
            result.SpaceBucks.Should().Be(0);
        }

        [TestMethod]
        public void Sample3()
        {
            IEnumerable<Event> events = new[] { new Event(5, 5), new Event(5, 1), new Event(6, 3), new Event(7, 2) };
            var result = Run(events, sampleRule3, Mode.Alpha, Variant.Foxtrot);
            result.Cookies.Should().Be(7, "5 cookies for event number 5 plus 2 cookies for event 6 should be 7 cookies!");
            result.Widgets.Should().Be(0);
            result.SpaceBucks.Should().Be(0);
        }

        [TestMethod]
        public void Sample4()
        {
            IEnumerable<Event> events = new[] { new Event(5, 5), new Event(5, 1), new Event(6, 3), new Event(7, 2), new Event(9, 1) };
            var result = Run(events, sampleRule4, Mode.Alpha, Variant.Foxtrot);
            result.Cookies.Should().Be(10, "6 cookies for event number 5 plus 2 cookies for event 6 plus two for event 9 should be 10 cookies!");
            result.Widgets.Should().Be(2, "2 widgets for event 7");
            result.SpaceBucks.Should().Be(0);
        }

    }
}
