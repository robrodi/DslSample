using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeskyScript
{
    public class Engine
    {
        public Output Run(IEnumerable<Event> events, string rules) 
        {
            return new Output(0, events.Count(), 0);
        }
    }
}
