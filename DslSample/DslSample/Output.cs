using System.Diagnostics.Contracts;
namespace DslSample
{
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

        [Pure]
        public static Output Create(int cookies, int spaceBucks, int widgets)
        {
            return new Output(cookies, spaceBucks, widgets);
        }

        [Pure]
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

}
