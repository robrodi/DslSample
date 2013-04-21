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
            public  readonly EventCriteria Criteria;
            public  readonly Condition Condition;
            private readonly object _value;
            public  Expression Value { get { return Expression.Constant(_value); } }

            public TestExpressionInfo(EventCriteria criteria, Condition condition, object value)
            {
                Contract.Requires(value != null);
                Criteria = criteria;
                Condition = condition;
                _value = value;
            }
        }

        [Pure]
        private static ConditionalExpression ProcessLine(ParameterExpression eventParameter, string line)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(line), "Empty line");
            Contract.Requires(eventParameter != null, "eventParameter is null");

            log.Debug("line: {0}", line);
            int slot = 0;
            var words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Contract.Assert(words.Length >= 6);

            // When
            Contract.Assert(words[slot++].Equals("when", StringComparison.OrdinalIgnoreCase), "rules should start with when");

            // Criteria
            var condition = new TestExpressionInfo(Parse<EventCriteria>(words[slot++]), Parse<Condition>(words[slot++]), uint.Parse(words[slot++]));

            // split point

            // operation
            Operation operation = Parse<Operation>(words[slot++]);

            var rewardApplied = words[slot++];

            int count;
            if (int.TryParse(rewardApplied, out count))
            {
                Contract.Assert(words.Length >= 7, "if specifying the number of rewards, you must have 7 tokens");
                Contract.Assert(count != 0, "you must not specify 0 count");
                rewardApplied = words[slot++];
            }


            log.Info("when {0} {1} {2}", condition.Criteria, condition, condition.Value);
            rewardApplied = rewardApplied.EndsWith("s") ? rewardApplied : rewardApplied + "s";

            var conditionalExpression = GetConditionExpression(condition, Expression.PropertyOrField(eventParameter, condition.Criteria.ToString()));

            // Operation
            var updateCount = GetOperationToUpdateCount(operation, eventParameter, count, rewardApplied);
            return Expression.IfThen(conditionalExpression, updateCount);
        }

        [Pure]
        private static TEnum Parse<TEnum>(string word) where TEnum : struct
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(word));
            TEnum value;
            Contract.Assert(Enum.TryParse(word, true, out value), string.Format("{0} missing.  Found {1}", typeof(TEnum).Name, word));
            return value;
        }

        private static Expression GetConditionExpression(TestExpressionInfo c, Expression eventField)
        {
            Func<Expression, Expression, Expression> comparer;
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

            return comparer(eventField, c.Value);
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
