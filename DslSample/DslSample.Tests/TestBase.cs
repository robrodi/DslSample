using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DslSample.Tests
{
    public abstract class TestBase
    {
        protected const string simplestRule = "when id is 3 add spacebuck";
        protected static Output Run(IEnumerable<Event> events, string rule = simplestRule, Mode mode = Mode.Charlie, Variant variant = Variant.Foxtrot)
        {
            var input = new Input(mode, variant);
            return new Engine(rule).Run(events, input);
        }
    }
}
