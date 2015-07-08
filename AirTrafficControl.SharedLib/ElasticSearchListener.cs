using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace AirTrafficControl.SharedLib
{
    public class ElasticSearchListener: EventListener, IDisposable
    {
        private ConcurrentEventSender<EventWrittenEventArgs> sender;

        public ElasticSearchListener()
        {
            this.sender = new ConcurrentEventSender<EventWrittenEventArgs>(
                eventBufferSize: 1000, 
                maxConcurrency: 5, 
                batchSize: 50, 
                noEventsDelay: TimeSpan.FromMilliseconds(200), 
                transmitterProc: SendEventsAsync);
        }

        

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)~0);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            this.sender.SubmitEvent(eventData);
        }

        private async Task SendEventsAsync(IEnumerable<EventWrittenEventArgs> events, CancellationToken cancellationToken)
        {
            
        }
    }
}
