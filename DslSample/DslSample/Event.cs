using System.Diagnostics.Contracts;

namespace DslSample
{
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
