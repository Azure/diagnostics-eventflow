using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Nest;

namespace AirTrafficControl.SharedLib
{
    public class ElasticSearchListener: EventListener, IDisposable
    {
        private const string Dot = ".";
        private const string Dash = "-";

        // TODO: consider making it a (configuration) property of the listener
        private const string EventDocumentTypeName = "event";

        private ConcurrentEventSender<EventWrittenEventArgs> sender;
        private ElasticClient esClient;
        private string indexNamePrefix;
        private string lastIndexName;

        // TODO: support for multiple ES nodes/connection pools, for failover and load-balancing
        public ElasticSearchListener(Uri serverUri, string userName, string password, string indexNamePrefix)
        {
            this.sender = new ConcurrentEventSender<EventWrittenEventArgs>(
                eventBufferSize: 1000, 
                maxConcurrency: 5, 
                batchSize: 50, 
                noEventsDelay: TimeSpan.FromMilliseconds(200), 
                transmitterProc: SendEventsAsync);

            if (serverUri == null || !serverUri.IsAbsoluteUri)
            {
                throw new ArgumentException("serverUri must be a valid, absolute URI", "serverUri");
            }

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Invalid Elastic Search credentials");
            }

            this.indexNamePrefix = string.IsNullOrWhiteSpace(indexNamePrefix) ? string.Empty : indexNamePrefix + Dash;
            this.lastIndexName = null;

            var config = new ConnectionSettings(serverUri).SetBasicAuthentication(userName, password);
            this.esClient = new ElasticClient(config);
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
            if (events == null)
            {
                return;
            }

            try
            {
                string currentIndexName = GetIndexName();
                if (!string.Equals(currentIndexName, this.lastIndexName, StringComparison.Ordinal))
                {
                    await EnsureIndexExists(currentIndexName);
                    this.lastIndexName = currentIndexName;
                }

                var request = new BulkRequest();
                request.Refresh = true;

                var operations = new List<IBulkOperation>();
                foreach(EventWrittenEventArgs eventSourceEvent in events)
                {
                    EventData eventData = eventSourceEvent.ToEventData();
                    var operation = new BulkCreateOperation<EventData>(eventData);
                    operation.Index = currentIndexName;
                    operation.Type = EventDocumentTypeName;
                    operations.Add(operation);
                }

                request.Operations = operations;

                IBulkResponse response = await this.esClient.BulkAsync(request);
                // TODO: handle errors when response.IsValid is false
            }
            catch
            {
                // TODO: a strategy for handling errors from sending to ES
            }
        }

        private async Task EnsureIndexExists(string currentIndexName)
        {
            var existsResult = await this.esClient.IndexExistsAsync(currentIndexName);
            if (existsResult.Exists)
            {
                return;
            }
            
            var createIndexResult = await this.esClient.CreateIndexAsync(c => c.Index(currentIndexName));
            // TODO: explicitly check for and ignore "index already exists" error
        }

        private string GetIndexName()
        {
            var now = DateTimeOffset.UtcNow;
            var retval = this.indexNamePrefix + now.Year.ToString() + Dot + now.Month.ToString() + Dot + now.Day.ToString();
            return retval;
        }
    }
}
