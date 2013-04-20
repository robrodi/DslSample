using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;

namespace HeskyScript
{
    using System.Reflection;
    using Runner = Func<Event, Output>;
    public class Engine
    {
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

            var words = rules.Split(new[]{' '}, System.StringSplitOptions.RemoveEmptyEntries);
            Contract.Assert(words.Length > 4);

            // input param
            ParameterExpression param = Expression.Parameter(typeof(Event), "event");
            // result
            ParameterExpression result = Expression.Variable(typeof(Output), "result");

            var spaceBucks = Expression.Variable(typeof(int), "spacebucks");
            var cookies = Expression.Variable(typeof(int), "cookies");
            var widgets = Expression.Variable(typeof(int), "widgets");
            
            var id = uint.Parse(words[3]);
            var ruleValue = Expression.Constant(id);
            var creator = typeof(Output).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
            var log = typeof(Console).GetMethod("WriteLine", new[] { typeof(int) });

            var variables = new[] { spaceBucks, cookies, widgets, result };
            BlockExpression block = Expression.Block(
                variables,
                Expression.Assign(spaceBucks, Expression.Constant(0)),
                Expression.Assign(cookies, Expression.Constant(0)),
                Expression.Assign(widgets, Expression.Constant(0)),
    
                // evaluate rules
                Expression.IfThen(Expression.Equal(ruleValue, Expression.PropertyOrField(param, "id")), 
                    Expression.Assign(spaceBucks, Expression.Add(spaceBucks, Expression.Constant(1)))),
                Expression.Call(log,spaceBucks),
                Expression.Assign(result, Expression.Call(creator, cookies, spaceBucks, widgets))
            );

            return Expression.Lambda<Func<Event, Output>>(block, param).Compile();

            //var spacebucks = 0;

            //// build a method for a single event that returns the sum output
            //if (words[0].Equals("when", StringComparison.OrdinalIgnoreCase))
            //{
            //    // support only id
            //    // support only is
            //    var id = int.Parse(words[3]);

            //    // if event id = value
            //    Expression<Func<Event, bool>> idEquals = e => e.Id == id;

            //    // then do something
            //    // support only add
                
            //    // support only spacebuck
            //}


            
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
