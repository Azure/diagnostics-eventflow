using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Diagnostics.Tracing;

namespace AirTrafficControl.SharedLib
{
    [EventSource(Name = "MyCompany-AirTrafficControlApplication-DiagnosticsChannelEventSource")]
    internal sealed class DiagnosticChannelEventSource: EventSource
    {
        public static DiagnosticChannelEventSource Current = new DiagnosticChannelEventSource();

        [Event(1, Level = EventLevel.Error, Message ="Event buffer overflowed and some events were lost")]
        public void EventsLost(string contextInfo)
        {
            if (this.IsEnabled())
            {
                WriteEvent(1, contextInfo);
            }
        }

        [Event(2, Level = EventLevel.Error, Message ="Unexpected exception ocurred during event upload")]
        public void EventUploadFailed(string contextInfo, string exception)
        {
            if (this.IsEnabled())
            {
                WriteEvent(2, exception);
            }
        }

        [Event(3, Level = EventLevel.Error, Message ="ES returned an error for a request")]
        public void EsRequestError(string contextInfo, string request, string error, string exception, int status)
        {
            if (this.IsEnabled())
            {
                WriteEvent(3, request, error, exception, status);
            }
        }
    }
}
