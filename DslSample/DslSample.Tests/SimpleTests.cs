using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
namespace DslSample.Tests
{
    [TestClass]
    public class SimpleTests
    {
        [TestMethod]
        public void TestExpressionInfoConstructors()
        {
            var eventInfo = new TestExpressionInfo(EventCriteria.Id, Condition.Equal, "5");
            eventInfo.Condition.Should().Be(Condition.Equal);
            eventInfo.Event.Should().Be(EventCriteria.Id);
            eventInfo.Input.Should().Be(InputCriteria.None);
            eventInfo.Source.Should().Be(TestExpressionInfo.ComparisonSource.Event);

            var inputInfo = new TestExpressionInfo(InputCriteria.Mode, Condition.Equal, "5");
            inputInfo.Condition.Should().Be(Condition.Equal);
            inputInfo.Event.Should().Be(EventCriteria.None);
            inputInfo.Input.Should().Be(InputCriteria.Mode);
            inputInfo.Source.Should().Be(TestExpressionInfo.ComparisonSource.Input);
        }

        [TestMethod]
        public void OutputToString()
        {
            new Output(1, 2, 3).ToString().Should().Be("1 cookies. 2 spacebucks. 3 widgets.");
        }
    }
}
