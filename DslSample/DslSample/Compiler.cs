using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DslSample
{
    using Runner = Func<Input, Event, Output>;

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
        readonly string[] comments = new[] { "#", "//" };
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

            // input params
            ParameterExpression input = Expression.Parameter(typeof(Input), "input");
            Contract.Assert(input!= null);
            ParameterExpression param = Expression.Parameter(typeof(Event), "event");
            Contract.Assert(param != null);

            // result
            ParameterExpression result = Expression.Variable(typeof(Output), "result");

            List<Expression> expressions = new List<Expression>();
            expressions.AddRange(OutputTypes.Select(t => Expression.Assign(t.Value, Expression.Constant(0))));

            foreach (var line in rules.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.IsNullOrWhiteSpace(line) || comments.Any(c => line.StartsWith(c)))
                {
                    log.Debug("Comment Line: {0}", line);
                    continue;
                }
                log.Debug("Processing line");
                var operation = ProcessLine(param, line, input);
                expressions.Add(operation);
                log.Debug("Done w/ line");
            }

            var creator = typeof(Output).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
            expressions.Add(Expression.Assign(result, Expression.Call(creator, OutputTypes.Values)));
            BlockExpression block = Expression.Block(
                OutputTypes.Values.Concat(new[] { result }),
                expressions
            );

            return Expression.Lambda<Func<Input, Event, Output>>(block, input, param).Compile();
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

        private static bool IsOperation(string word)
        {
            return !string.IsNullOrWhiteSpace(word) && Enum.GetNames(typeof(Operation)).Any(w => w.Equals(word, StringComparison.OrdinalIgnoreCase));
        }
        [Pure]
        private static ConditionalExpression ProcessLine(ParameterExpression eventParameter, string line, ParameterExpression inputParameter)
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
            while(slot < words.Length && !IsOperation(words[slot]))
            {
                Contract.Assert(words.Length > slot + 3);
                if (!firstCondition) slot++;
                firstCondition = false;
                condition = TestExpressionInfo.Parse(words[slot++], EnumParsing.Parse<Condition>(words[slot++]), words[slot++]);
                var conditionalExpression = GetConditionExpression(condition, eventParameter, inputParameter);
                criteria = BinaryExpression.And(criteria, conditionalExpression);
            }

            // operation
            Operation operation = EnumParsing.Parse<Operation>(words[slot++]);

            Contract.Assert(words.Length > slot);
            var rewardApplied = words[slot++];

            int count;
            if (int.TryParse(rewardApplied, out count))
            {
                Contract.Assert(words.Length >= slot + 1, "if specifying the number of rewards, you must have 7 tokens");
                Contract.Assert(count != 0, "you must not specify 0 count");
                rewardApplied = words[slot++];
            }

            log.Info("when {0} {1} {2}", condition.Event, condition.Condition, condition.Value);
            rewardApplied = rewardApplied.EndsWith("s") ? rewardApplied : rewardApplied + "s";

            // Operation
            var updateCount = GetOperationToUpdateCount(operation, eventParameter, count, rewardApplied);
            return Expression.IfThen(criteria, updateCount);
        }

        private static BinaryExpression GetConditionExpression(TestExpressionInfo c, Expression eventParameter, Expression inputParameter)
        {
            Contract.Requires(eventParameter != null);
            Contract.Requires(inputParameter != null);

            Func<Expression, Expression, BinaryExpression> comparer;
            switch (c.Condition)
            {
                case Condition.Is:
                case Condition.Equal:
                case Condition.Eq:
                    log.Debug("Equal");
                    comparer = Expression.Equal;
                    break;
                case Condition.NotEqual:
                case Condition.Neq:
                    log.Debug("NotEqual");
                    comparer = Expression.NotEqual;
                    break;
                case Condition.Greater:
                case Condition.GreaterThan:
                case Condition.Gt:
                    log.Debug("GreaterThan");
                    comparer = Expression.GreaterThan;
                    break;
                case Condition.Less:
                case Condition.LessThan:
                case Condition.Lt:
                    log.Debug("LessThan");
                    comparer = Expression.LessThan;
                    break;
                case Condition.GreaterThanOrEqual:
                case Condition.Gte:
                    log.Debug("GreaterThan or Equal");
                    comparer = BinaryExpression.GreaterThanOrEqual;
                    break;
                case Condition.LessThanOrEqual:
                case Condition.Lte:
                    log.Debug("LessThan or Equal");
                    comparer = BinaryExpression.LessThanOrEqual;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("c", c, "Invalid condition");
            }

            var comparisonSource = c.Source == TestExpressionInfo.ComparisonSource.Event ? eventParameter : inputParameter;
            var comparisonParameterName = c.Source == TestExpressionInfo.ComparisonSource.Event ? c.Event.ToString() : c.Input.ToString();
            var comparisonSourceParameter = Expression.PropertyOrField(comparisonSource, comparisonParameterName);
            return comparer(comparisonSourceParameter, c.Value);
        }

        [Pure]
        private static BinaryExpression GetOperationToUpdateCount(Operation op, ParameterExpression eventParam, int count, string rewardToApply)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(rewardToApply), "Invalid reward");
            Contract.Assert(OutputTypes.ContainsKey(rewardToApply.ToLower()), "Cannot find key: " + rewardToApply);
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

            log.Info("{0} {1} {2}", op, useEventCount ? "event.Count" : count.ToString(), rewardToApply);
            
            var addition = operation(counter, ammountToAdd);
            return Expression.Assign(counter, addition);
        }

        //[Obsolete("not yet used")]
        //static Expression Debug2(string format, params Expression[] parameters)
        //{
        //    Contract.Requires(!string.IsNullOrWhiteSpace(format), "bad log");

        //    Expression[] paramList = new[] { Expression.Constant(format) };
        //    var expressions = parameters != null && parameters.Length>0 ? paramList.Concat(parameters) : paramList;
        //    return Expression.Call(typeof(Logger), "Debug", new[] { typeof(string), typeof(object[]) }, expressions.ToArray());
        //}
    }

}
