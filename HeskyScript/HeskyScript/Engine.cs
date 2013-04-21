using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using NLog;
namespace HeskyScript
{
    using System.Reflection;
    using Runner = Func<Event, Output>;
    public class Engine
    {
        static Logger l = LogManager.GetCurrentClassLogger();
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
            var rule = Compile(_rule);
            return events.Select(rule).Aggregate(Output.Add);
        }

        [Pure]
        static Runner Compile(string rules)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(rules));
            l.Trace("Rules: {0}", rules);
            var lines = rules.Split(new[]{Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);

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

        private static ConditionalExpression ProcessLine(Dictionary<string, ParameterExpression> types, ParameterExpression eventParameter, string line)
        {
            l.Debug("line: {0}", line);

            var words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Contract.Assert(words.Length >= 6);
            var id = uint.Parse(words[3]);
            var ruleValue = Expression.Constant(id);
            var ruleFieldName = words[5].ToLower();

            ruleFieldName = ruleFieldName.EndsWith("s") ? ruleFieldName : ruleFieldName + "s";
            l.Debug("ID: {0} RFN: {1}", id, ruleFieldName);

            if (!types.ContainsKey(ruleFieldName.ToLower())) throw new ArgumentException("Cannot find key: " + ruleFieldName);

            var updateCount = GetOperationToUpdateCount(Operation.Add, types[ruleFieldName], eventParameter);
            var operation = Expression.IfThen(Expression.Equal(ruleValue, Expression.PropertyOrField(eventParameter, "id")), updateCount);
            return operation;
        }

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
                return Engine.Compile(_engine._rule);
            }
        }
    }
}
