using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Elasticsearch.Net.Connection;

namespace AirTrafficControl.SharedLib
{
    public class ElasticSearchListener: EventListener, IDisposable
    {
        private const string Dot = ".";
        private const string Dash = "-";

        private ConcurrentEventSender<EventWrittenEventArgs> sender;
        private ElasticsearchClient esClient;
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

            var config = new ConnectionConfiguration(serverUri).SetBasicAuthentication(userName, password);
            this.esClient = new ElasticsearchClient(config);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)~0);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            this.sender.SubmitEvent(eventData);
        }

        private Task SendEventsAsync(IEnumerable<EventWrittenEventArgs> events, CancellationToken cancellationToken)
        {
            try
            {
                string currentIndexName = GetIndexName();
                if (!string.Equals(currentIndexName, this.lastIndexName, StringComparison.Ordinal))
                {
                    EnsureIndexExists(currentIndexName);
                }
            }
            catch
            {
                // TODO: a strategy for handling errors from sending to ES
            }
        }

        private async Task EnsureIndexExists(string currentIndexName)
        {
            var result = await this.esClient.IndicesExistsAsync(currentIndexName);
            if ((bool) result.Response["Exists"])
            {
                return;
            }

            // TODO: explicitly check and ignore 
            var settings = 
            await this.esClient.IndicesCreateAsync(currentIndexName);
        }

        private string GetIndexName()
        {
            var now = DateTimeOffset.UtcNow;
            var retval = this.indexNamePrefix + now.Year.ToString() + Dot + now.Month.ToString() + Dot + now.Day.ToString();
            return retval;
        }
    }
}
