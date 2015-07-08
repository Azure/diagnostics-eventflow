using System;
using System.Diagnostics.Tracing;

namespace AirTrafficControl.SharedLib
{
    public class EventHubListener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords) ~0);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            // TODO
        }
    }
}
