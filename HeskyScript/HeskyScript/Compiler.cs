using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Runner = System.Func<HeskyScript.Event, HeskyScript.Output>;

namespace HeskyScript
{
    /// <summary>
    /// Compiles the rules.
    /// </summary>
    internal class Compiler
    {
        // nlog logger.
        private static Logger log = LogManager.GetCurrentClassLogger();

        // logger for the running class.
        [Obsolete("not yet used")]
        static Logger runningLogger = LogManager.GetLogger("running");

        #region helpers
        private static readonly MethodInfo toInt = typeof(Convert).GetMethod("ToInt32", new[] { typeof(uint) });
        private static IDictionary<string, ParameterExpression> _types;
        [Obsolete("not yet used")]
        private static readonly MethodInfo debug = typeof(Logger).GetMethod("Trace", new[] { typeof(string), typeof(object[])});
        #endregion

        /// <summary>
        /// Compiles the string rule into an expression tree for generating rewards from an event.
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        [Pure]
        internal Runner Compile(string rules)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(rules));
            log.Trace("Rules: {0}", rules);
            // input param
            ParameterExpression param = Expression.Parameter(typeof(Event), "event");
            Contract.Assert(param != null);

            // result
            ParameterExpression result = Expression.Variable(typeof(Output), "result");

            List<Expression> expressions = new List<Expression>();
            expressions.AddRange(OutputTypes.Select(t => Expression.Assign(t.Value, Expression.Constant(0))));

            foreach (var line in rules.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                var operation = ProcessLine(param, line);
                expressions.Add(operation);
            }

            var creator = typeof(Output).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
            expressions.Add(Expression.Assign(result, Expression.Call(creator, OutputTypes.Values)));
            BlockExpression block = Expression.Block(
                OutputTypes.Values.Concat(new[] { result }),
                expressions
            );

            return Expression.Lambda<Func<Event, Output>>(block, param).Compile();
        }

        private static IDictionary<string, ParameterExpression> OutputTypes
        {
            get
            {
                if (_types != null) return _types;

                return _types = typeof(Output).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Select(p => p.Name.ToLower()).ToDictionary(e => e, e => Expression.Variable(typeof(int), e));
            }
        }

        struct TestExpressionInfo
        {
            public readonly EventCriteria EventCriteria;
            public readonly GlobalCriteria GlobalCriteria;
            public  readonly Condition Condition;
            private readonly object _value;
            public  Expression Value { get { return Expression.Constant(_value); } }

            public TestExpressionInfo(EventCriteria criteria, Condition condition, object value) : 
                this(criteria, GlobalCriteria.None, condition, value)
            {
            }
            public TestExpressionInfo(GlobalCriteria criteria, Condition condition, object value) :
                this(EventCriteria.None, criteria, condition, value)
            {
            }
            private TestExpressionInfo(EventCriteria eventCriteria, GlobalCriteria globalCriteria, Condition condition, object value) 
            {
                Contract.Requires(value != null);
                Contract.Requires(eventCriteria != HeskyScript.EventCriteria.None || globalCriteria != HeskyScript.GlobalCriteria.None);
                Contract.Requires(eventCriteria == HeskyScript.EventCriteria.None || globalCriteria == HeskyScript.GlobalCriteria.None);

                EventCriteria = eventCriteria;
                GlobalCriteria = globalCriteria;
                Condition = condition;
                _value = value;
            }
            internal static TestExpressionInfo Parse(string critiera, Condition condition, string value)
            {
                Contract.Requires(!string.IsNullOrWhiteSpace(critiera));
                Contract.Requires(value != null);

                var g = TryParse<GlobalCriteria>(critiera);
                var e = TryParse<EventCriteria>(critiera);

                object val = null;
                if (e.HasValue)
                {
                    val = UInt32.Parse(value);
                }
                if (g.HasValue)
                {
                    switch (g.Value) 
                    {
                        case GlobalCriteria.Mode:
                            val = Parse<Mode>(value);
                            break;
                        case GlobalCriteria.Variant:
                            val = Parse<Variant>(value);
                            break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
                

                return new TestExpressionInfo(e.HasValue ? e.Value : EventCriteria.None, 
                                                g.HasValue ? g.Value : GlobalCriteria.None, 
                                                condition, 
                                                val);
            }
        }

        private static bool IsOperation(string word)
        {
            return !string.IsNullOrWhiteSpace(word) && Enum.GetNames(typeof(Operation)).Any(w => w.Equals(word, StringComparison.OrdinalIgnoreCase));
        }
        [Pure]
        private static ConditionalExpression ProcessLine(ParameterExpression eventParameter, string line)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(line), "Empty line");
            Contract.Requires(eventParameter != null, "eventParameter is null");

            int slot = 0;
            var words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Contract.Assert(words.Length >= 2);

            // When
            if (words[0].Equals("when", StringComparison.OrdinalIgnoreCase)) slot++;

            TestExpressionInfo condition =new  TestExpressionInfo();

            Expression criteria = Expression.Constant(true);

            bool firstCondition = true;
            while(!IsOperation(words[slot]))
            {
                Contract.Assert(words.Length > slot + 3);
                if (!firstCondition) slot++;
                firstCondition = false;
                condition = TestExpressionInfo.Parse(words[slot++], Parse<Condition>(words[slot++]), words[slot++]);
                var conditionalExpression = GetConditionExpression(condition, eventParameter);
                criteria = BinaryExpression.And(criteria, conditionalExpression);
            }

            // operation
            Operation operation = Parse<Operation>(words[slot++]);

            Contract.Assert(words.Length > slot);
            var rewardApplied = words[slot++];

            int count;
            if (int.TryParse(rewardApplied, out count))
            {
                Contract.Assert(words.Length >= slot + 1, "if specifying the number of rewards, you must have 7 tokens");
                Contract.Assert(count != 0, "you must not specify 0 count");
                rewardApplied = words[slot++];
            }


            log.Info("when {0} {1} {2}", condition.EventCriteria, condition, condition.Value);
            rewardApplied = rewardApplied.EndsWith("s") ? rewardApplied : rewardApplied + "s";


            // Operation
            var updateCount = GetOperationToUpdateCount(operation, eventParameter, count, rewardApplied);
            return Expression.IfThen(criteria, updateCount);
        }

        [Pure]
        private static TEnum? TryParse<TEnum>(string word) where TEnum : struct
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(word));
            TEnum value;
            var success = Enum.TryParse(word, true, out value);
            log.Debug("TryParse: {0} success? {1}.  Input: <{2}>", typeof(TEnum).Name, success, word);
            return success ? value : (TEnum?)null;
        }
        [Pure]
        private static TEnum Parse<TEnum>(string word) where TEnum : struct
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(word));
            TEnum value;
            Contract.Assert(Enum.TryParse(word, true, out value), string.Format("{0} missing.  Found <{1}>", typeof(TEnum).Name, word));
            return value;
        }

        private static BinaryExpression GetConditionExpression(TestExpressionInfo c, Expression eventParameter)
        {
            Func<Expression, Expression, BinaryExpression> comparer;
            switch (c.Condition)
            {
                case Condition.Is:
                    log.Debug("Equal");
                    comparer = Expression.Equal;
                    break;
                case Condition.NotEqual:
                    log.Debug("NotEqual");
                    comparer = Expression.NotEqual;
                    break;
                case Condition.Greater:
                case Condition.GreaterThan:
                case Condition.gt:
                    log.Debug("GreaterThan");
                    comparer = Expression.GreaterThan;
                    break;
                case Condition.Less:
                case Condition.LessThan:
                case Condition.lt:
                    log.Debug("LessThan");
                    comparer = Expression.LessThan;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("c", c, "Invalid condition");
            }

            return comparer(Expression.PropertyOrField(eventParameter, c.EventCriteria.ToString()), c.Value);
        }

        [Pure]
        private static BinaryExpression GetOperationToUpdateCount(Operation op, ParameterExpression eventParam, int count, string rewardToApply)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(rewardToApply), "Invalid reward");
            if (!OutputTypes.ContainsKey(rewardToApply.ToLower())) throw new ArgumentException("Cannot find key: " + rewardToApply);
            var counter = OutputTypes[rewardToApply.ToLower()];

            Func<Expression, Expression, BinaryExpression> operation = op ==
                Operation.Add ?
                    (Func<Expression, Expression, BinaryExpression>)Expression.Add :
                    (Func<Expression, Expression, BinaryExpression>)Expression.Subtract;
            var useEventCount = (count == 0);
            log.Debug("Using Event count? : {0}.  count: {1}", useEventCount, count);
            Expression ammountToAdd = useEventCount ?
                (Expression)Expression.Call(toInt, Expression.PropertyOrField(eventParam, "Count")) :
                (Expression)Expression.Constant(count);

            log.Info("{0} {1} {2}", op, useEventCount ? "eventCount" : count.ToString(), rewardToApply);
            
            var addition = operation(counter, ammountToAdd);
            return Expression.Assign(counter, addition);
        }

        [Obsolete("not yet used")]
        static Expression Debug2(string format, params Expression[] parameters)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(format), "bad log");

            Expression[] paramList = new[] { Expression.Constant(format) };
            var expressions = parameters != null && parameters.Length>0 ? paramList.Concat(parameters) : paramList;
            return Expression.Call(typeof(Logger), "Debug", new[] { typeof(string), typeof(object[]) }, expressions.ToArray());
        }

    }

}
