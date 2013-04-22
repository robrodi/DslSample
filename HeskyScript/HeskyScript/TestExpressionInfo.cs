using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HeskyScript
{
    struct TestExpressionInfo
    {
        public enum ComparisonSource
        {
            Event,
            Input
        }

        public readonly EventCriteria Event;
        public readonly InputCriteria Input;
        public readonly Condition Condition;
        public readonly ComparisonSource Source;
        private readonly object _value;

        public Expression Value { get { return Expression.Constant(_value); } }

        public TestExpressionInfo(EventCriteria criteria, Condition condition, object value) :
            this(criteria, InputCriteria.None, condition, value)
        {
            Contract.Requires(value != null);
        }
        public TestExpressionInfo(InputCriteria criteria, Condition condition, object value) :
            this(EventCriteria.None, criteria, condition, value)
        {
            Contract.Requires(value != null);
        }

        private TestExpressionInfo(EventCriteria eventCriteria, InputCriteria globalCriteria, Condition condition, object value)
        {
            Contract.Requires(value != null);
            
            Event = eventCriteria;
            Input = globalCriteria;
            Condition = condition;
            _value = value;
            Source = Get(globalCriteria, eventCriteria);
        }

        internal static TestExpressionInfo Parse(string critiera, Condition condition, string value)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(critiera));
            Contract.Requires(value != null);

            var g = EnumParsing.TryParse<InputCriteria>(critiera);
            var e = EnumParsing.TryParse<EventCriteria>(critiera);

            object val = null;
            if (e.HasValue)
            {
                val = UInt32.Parse(value);
            }
            if (g.HasValue)
            {
                switch (g.Value)
                {
                    case InputCriteria.Mode:
                        val = EnumParsing.Parse<Mode>(value);
                        break;
                    case InputCriteria.Variant:
                        val = EnumParsing.Parse<Variant>(value);
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }

            Contract.Assert(val != null);

            return new TestExpressionInfo(e.HasValue ? e.Value : EventCriteria.None,
                                            g.HasValue ? g.Value : InputCriteria.None,
                                            condition,
                                            val);
        }

        [Pure]
        private static ComparisonSource Get(InputCriteria input, EventCriteria e)
        {
            // cant both be null and can't both be not null.
            Contract.Requires(e != EventCriteria.None || input != InputCriteria.None);
            Contract.Requires(e == EventCriteria.None || input == InputCriteria.None);

            return e == EventCriteria.None ? ComparisonSource.Input : ComparisonSource.Event;
        }
    }
}
