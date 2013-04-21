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
    internal class Compiler
    {
        static Logger log = LogManager.GetCurrentClassLogger();
        static Logger runningLogger = LogManager.GetLogger("running");

        #region helpers
        private static readonly MethodInfo toInt = typeof(Convert).GetMethod("ToInt32", new[] { typeof(uint) });
        private static readonly IDictionary<string, ParameterExpression> types = GetOutputTypes();
        #endregion

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
            expressions.AddRange(types.Select(t => Expression.Assign(t.Value, Expression.Constant(0))));

            foreach (var line in rules.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                var operation = ProcessLine(param, line);
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
            EventCriteria c = Parse<EventCriteria>(words[slot++]);

            // Condition
            Condition condition = Parse<Condition>(words[slot++]);

            // value
            var id = uint.Parse(words[slot++]);
            var ruleValue = Expression.Constant(id);

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


            log.Info("when {0} {1} {2}", c, condition, id);
            rewardApplied = rewardApplied.EndsWith("s") ? rewardApplied : rewardApplied + "s";

            var conditionalExpression = GetConditionExpression(condition, Expression.PropertyOrField(eventParameter, c.ToString()), ruleValue);

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
        private static BinaryExpression GetOperationToUpdateCount(Operation op, ParameterExpression eventParam, int count, string rewardToApply)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(rewardToApply), "Invalid reward");
            if (!types.ContainsKey(rewardToApply.ToLower())) throw new ArgumentException("Cannot find key: " + rewardToApply);
            var counter = types[rewardToApply.ToLower()];

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

    }

}
