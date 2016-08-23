using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Diagnostics.Consumers.ConsoleApp
{
    [EventSource(Name = "MyEventSource")]
    internal class MyEventSource : EventSource
    {
        public static MyEventSource Log = new MyEventSource();

        [Event(1, Level = EventLevel.Informational)]
        public void Message(string message)
        {
            WriteEvent(1, message);
        }
    }
}
