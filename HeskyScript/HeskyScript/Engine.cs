using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Runner = System.Func<HeskyScript.Event, HeskyScript.Output>;

using NLog;

namespace HeskyScript
{
    internal class Compiler
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        #region helpers
        private static readonly MethodInfo toInt = typeof(Convert).GetMethod("ToInt32", new[] { typeof(uint) });

        #endregion

        [Pure]
        internal Runner Compile(string rules)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(rules));
            log.Trace("Rules: {0}", rules);

            // input param
            ParameterExpression param = Expression.Parameter(typeof(Event), "event");
            // result
            ParameterExpression result = Expression.Variable(typeof(Output), "result");

            var types = GetOutputTypes();
            List<Expression> expressions = new List<Expression>();
            expressions.AddRange(types.Select(t => Expression.Assign(t.Value, Expression.Constant(0))));

            foreach (var line in rules.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                var operation = ProcessLine(types, param, line);
                expressions.Add(operation);
            }

            var creator = typeof(Output).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
            expressions.Add(Expression.Assign(result, Expression.Call(creator, types.Values)));
            BlockExpression block = Expression.Block(
                types.Values.Concat(new[] { result }),
                expressions
            );

            return Expression.Lambda<Func<Event, Output>>(block, param).Compile();
        }

        private static Dictionary<string, ParameterExpression> GetOutputTypes()
        {
            return typeof(Output).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => p.Name.ToLower()).ToDictionary(e => e, e => Expression.Variable(typeof(int), e));
        }

        [Pure]
        private static ConditionalExpression ProcessLine(Dictionary<string, ParameterExpression> types, ParameterExpression eventParameter, string line)
        {
            log.Debug("line: {0}", line);
            int slot = 0;
            var words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Contract.Assert(words.Length >= 6);

            // When
            Contract.Assert(words[slot++].Equals("when", StringComparison.OrdinalIgnoreCase), "rules should start with when");

            // Criteria
            EventCriteria c = Parse<EventCriteria>(words[slot++]);

            // Condition
            Condition condition = Parse<Condition>(words[slot++]);

            // value
            var id = uint.Parse(words[slot++]);
            var ruleValue = Expression.Constant(id);

            // operation
            Operation operation = Parse<Operation>(words[slot++]);

            var rewardApplied = words[slot++];

            int count = int.MinValue;
            if (int.TryParse(rewardApplied, out count))
            {
                Contract.Assert(words.Length >= 7, "if specifying the number of rewards, you must have 7 tokens");
                rewardApplied = words[slot++];
            }

            rewardApplied = rewardApplied.EndsWith("s") ? rewardApplied : rewardApplied + "s";


            var comparedTo = Expression.PropertyOrField(eventParameter, c.ToString());
            var x = GetConditionExpression(condition, comparedTo, ruleValue);

            // Operation
            if (!types.ContainsKey(rewardApplied.ToLower())) throw new ArgumentException("Cannot find key: " + rewardApplied);
            var updateCount = GetOperationToUpdateCount(operation, types[rewardApplied.ToLower()], eventParameter, count);

            return Expression.IfThen(x, updateCount);
        }

        [Pure]
        private static TEnum Parse<TEnum>(string word) where TEnum : struct
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(word));
            TEnum value;
            Contract.Assert(Enum.TryParse(word, true, out value), string.Format("{0} missing.  Found {1}", typeof(TEnum).Name, word));
            return value;
        }

        private static Expression GetConditionExpression(Condition c, Expression l, ConstantExpression r)
        {
            Func<Expression, Expression, Expression> comparer;
            switch (c)
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

            return comparer(l, r);
        }



        [Pure]
        private static BinaryExpression GetOperationToUpdateCount(Operation op, Expression counter, ParameterExpression eventParam, int count)
        {


            Func<Expression, Expression, BinaryExpression> operation = op ==
                Operation.Add ?
                    (Func<Expression, Expression, BinaryExpression>)Expression.Add :
                    (Func<Expression, Expression, BinaryExpression>)Expression.Subtract;
            var useEventCount = (count == 0 || count == int.MinValue);
            log.Debug("Using count? : {0}.  count: {1}", useEventCount, count);
            Expression ammountToAdd = useEventCount ?
                (Expression)Expression.Call(toInt, Expression.PropertyOrField(eventParam, "Count")) :
                (Expression)Expression.Constant(count);
            var addition = operation(counter, ammountToAdd);
            return Expression.Assign(counter, addition);
        }

    }

    public class Engine
    {
        static Logger l = LogManager.GetCurrentClassLogger();
        readonly string _rule;
        readonly Runner _runner;

        public Engine( string rule)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(rule));
            _rule = rule;
            _runner = new Compiler().Compile(_rule);
        }

        [Pure]
        public Output Run(IEnumerable<Event> events)
        {
            Contract.Requires(events != null);
            return events.Select(_runner).Aggregate(Output.Add);
        }

        public class TestWrapper
        {
            Engine _engine;
            public TestWrapper(Engine engine) {
                Contract.Requires(engine != null);
                _engine = engine; 
            }
            public Runner Compile()
            {
                return _engine._runner;
            }
        }
    }
}
