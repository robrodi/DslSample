using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeskyScript
{
    public class Engine
    {
        readonly Mode _mode;
        readonly Variant _variant;
        readonly string _rule;

        public Engine(Mode mode, Variant variant, string rule)
        {
            _mode = mode;
            _variant = variant;
            _rule = rule;
        }

        public Output Run(IEnumerable<Event> events)
        {
            return new Output(0, events.Count(), 0);
        }
    }
}
