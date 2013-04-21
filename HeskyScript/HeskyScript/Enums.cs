using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeskyScript
{
    public enum Keywords
    {
        When,
        And,
        Or
    }

    public enum Condition
    {
        Is,
        NotEqual,
        Greater,
        GreaterThan,
        gt,
        Less,
        LessThan,
        lt
    }

    public enum EventCriteria
    {
        Id,
        Count
    }

    public enum GlobalCriteria
    {
        Mode,
        Variant
    }

    public enum Operation
    {
        Add,
        Subtract
    }
    public enum Mode
    {
        Alpha = 1,
        Bravo,
        Charlie,
        Delta
    }

    public enum Variant
    {
        Echo,
        Foxtrot,
        Golf,
        Hotel
    }

    public struct Output
    {
        private readonly int _cookies, _spaceBucks, _widgets;

        public int Cookies { get { return _cookies; } }
        public int SpaceBucks { get { return _spaceBucks; } }
        public int Widgets { get { return _widgets; } }

        public Output(int cookies = 0, int spaceBucks = 0, int widgets = 0)
        {
            _cookies = cookies;
            _spaceBucks = spaceBucks;
            _widgets = widgets;
        }

        public static Output Create(int cookies, int spaceBucks, int widgets)
        {
            var result = new Output(cookies, spaceBucks, widgets);
            Console.WriteLine(result);
            return result;
        }

        public override string ToString()
        {
            return string.Format("{0} cookies. {1} spacebucks. {2} widgets.", this._cookies, this._spaceBucks, this._widgets);
        }

        [Pure]
        public static Output Add(Output l, Output r)
        {
            return new Output(l.Cookies + r.Cookies, l.SpaceBucks + r.SpaceBucks, l.Widgets + r.Widgets);
        }
    }

    public struct Event
    {
        private readonly uint _id;
        private readonly uint _count;

        public uint Id { get { return _id; } }
        public uint Count { get { return _count; } }

        public Event(uint id, uint count)
        {
            Contract.Requires(count > 0);
            _id = id;
            _count = count;
        }
    }
}

