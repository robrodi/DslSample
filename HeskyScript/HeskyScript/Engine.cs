using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace HeskyScript
{
    public class Engine
    {
        readonly Mode _mode;
        readonly Variant _variant;
        readonly string _rule;

        public Engine(Mode mode, Variant variant, string rule)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(rule));
            _mode = mode;
            _variant = variant;
            _rule = rule;
        }

        [Pure]
        public Output Run(IEnumerable<Event> events)
        {
            Contract.Requires(events != null);
            return new Output(0, events.Count(), 0);
        }
    }
}
