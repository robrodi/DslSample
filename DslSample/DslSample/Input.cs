using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DslSample
{
    public struct Input
    {
        public readonly Mode Mode;
        public readonly Variant Variant;
        public Input(Mode mode, Variant variant)
        {
            Mode = mode;
            Variant = variant;
        }
    }
}
