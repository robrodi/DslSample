using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace DslSample
{
    using Runner = Func<Input, Event, Output>;

    public class Engine
    {
        readonly string _rule;
        readonly Runner _runner;

        public Engine( string rule)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(rule));
            _rule = rule;
            _runner = new Compiler().Compile(_rule);
        }

        [Pure]
        public Output Run(IEnumerable<Event> events, Input input)
        {
            Contract.Requires(events != null);
            return events.Select(e => _runner(input, e)).Aggregate(Output.Add);
        }

        public class TestWrapper
        {
            readonly Engine _engine;
            
            public TestWrapper(Engine engine) {
                Contract.Requires(engine != null);
                _engine = engine; 
            }

            [Pure]
            public Runner Compile()
            {
                return _engine._runner;
            }
        }
    }
}
