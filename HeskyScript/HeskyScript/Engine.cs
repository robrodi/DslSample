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
        static Logger l = LogManager.GetCurrentClassLogger();

        [Pure]
        internal Runner Compile(string rules)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(rules));
            l.Trace("Rules: {0}", rules);
            var lines = rules.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            var types = typeof(Output).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name.ToLower()).ToDictionary(e => e, e => Expression.Variable(typeof(int), e));

            // input param
            ParameterExpression param = Expression.Parameter(typeof(Event), "event");
            // result
            ParameterExpression result = Expression.Variable(typeof(Output), "result");

            var variables = types.Values.Concat(new[] { result });
            List<Expression> expressions = new List<Expression>();
            expressions.AddRange(types.Select(t => Expression.Assign(t.Value, Expression.Constant(0))));

            foreach (var line in lines)
            {
                var operation = ProcessLine(types, param, line);
                expressions.Add(operation);
            }

            var creator = typeof(Output).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
            expressions.Add(Expression.Assign(result, Expression.Call(creator, types.Values)));
            BlockExpression block = Expression.Block(
                variables,
                expressions
            );

            return Expression.Lambda<Func<Event, Output>>(block, param).Compile();
        }

        [Pure]
        private static ConditionalExpression ProcessLine(Dictionary<string, ParameterExpression> types, ParameterExpression eventParameter, string line)
        {
            l.Debug("line: {0}", line);

            var words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Contract.Assert(words.Length >= 6);
            Contract.Assert(words[0].Equals("when", StringComparison.OrdinalIgnoreCase), "rules should start with when");

            // Parse criteria
            Criteria c;
            Contract.Assert(Enum.TryParse<Criteria>(words[1], true, out c), string.Format("criteria missing.  Found {0}", words[1]));

            var id = uint.Parse(words[3]);
            var ruleValue = Expression.Constant(id);
            var ruleFieldName = words[5].ToLower();

            ruleFieldName = ruleFieldName.EndsWith("s") ? ruleFieldName : ruleFieldName + "s";
            l.Debug("ID: {0} RFN: {1}", id, ruleFieldName);

            if (!types.ContainsKey(ruleFieldName.ToLower())) throw new ArgumentException("Cannot find key: " + ruleFieldName);

            var updateCount = GetOperationToUpdateCount(Operation.Add, types[ruleFieldName], eventParameter);
            var comparedTo = Expression.PropertyOrField(eventParameter, c.ToString());
            var operation = Expression.IfThen(Expression.Equal(ruleValue, comparedTo), updateCount);
            return operation;
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
        //readonly Mode _mode;
        //readonly Variant _variant;
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

        [Pure]
        private static Output sample(Event e)
        {
            int spacebucks = 0;
            if (e.Id == 3)
                spacebucks++;
            return new Output(spaceBucks: spacebucks);
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
