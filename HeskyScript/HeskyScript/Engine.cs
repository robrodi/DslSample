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

            var words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Contract.Assert(words.Length >= 6);

            // When
            Contract.Assert(words[0].Equals("when", StringComparison.OrdinalIgnoreCase), "rules should start with when");

            // Criteria
            EventCriteria c = Parse<EventCriteria>(words[1]);

            // Condition
            Condition condition = Parse<Condition>(words[2]);

            // value
            var value = words[3];
            var id = uint.Parse(words[3]);
            var ruleValue = Expression.Constant(id);



            var ruleFieldName = words[5].ToLower();

            ruleFieldName = ruleFieldName.EndsWith("s") ? ruleFieldName : ruleFieldName + "s";
            log.Debug("RFN: {2} Op: {1} Rule Value: {0} ", id, c, ruleFieldName);

            if (!types.ContainsKey(ruleFieldName.ToLower())) throw new ArgumentException("Cannot find key: " + ruleFieldName);




            var updateCount = GetOperationToUpdateCount(Operation.Add, types[ruleFieldName], eventParameter);
            var comparedTo = Expression.PropertyOrField(eventParameter, c.ToString());
            var x = GetConditionExpression(condition, comparedTo, ruleValue);
            var operation = Expression.IfThen(x, updateCount);
            return operation;
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
        private static BinaryExpression GetOperationToUpdateCount(Operation op, Expression counter, ParameterExpression eventParam)
        {
            var toInt = typeof(Convert).GetMethod("ToInt32", new[] { typeof(uint) });
            Func<Expression, Expression, BinaryExpression> operation = op ==
                Operation.Add ?
                    (Func<Expression, Expression, BinaryExpression>)Expression.Add :
                    (Func<Expression, Expression, BinaryExpression>)Expression.Subtract;
            var addition = operation(counter, Expression.Call(toInt, Expression.PropertyOrField(eventParam, "Count")));
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
