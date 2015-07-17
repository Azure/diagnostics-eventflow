using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Nest;
using System.Diagnostics;

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
        private string contextInfo;

        // TODO: support for multiple ES nodes/connection pools, for failover and load-balancing
        public ElasticSearchListener(string contextInfo, Uri serverUri, string userName, string password, string indexNamePrefix)
        {
            this.sender = new ConcurrentEventSender<EventWrittenEventArgs>(
                contextInfo: contextInfo,
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

            this.contextInfo = contextInfo;

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
                if (!response.IsValid)
                {
                    ReportEsRequestError(response, "Bulk upload");
                }
            }
            catch(Exception e)
            {
                DiagnosticChannelEventSource.Current.EventUploadFailed(this.contextInfo, e.ToString());
            }
        }

        private async Task EnsureIndexExists(string currentIndexName)
        {
            var existsResult = await this.esClient.IndexExistsAsync(currentIndexName);
            if (!existsResult.IsValid)
            {
                ReportEsRequestError(existsResult, "Index exists check");
                return;
            }

            if (existsResult.Exists)
            {
                return;
            }
            
            var createIndexResult = await this.esClient.CreateIndexAsync(c => c.Index(currentIndexName));
            if (!createIndexResult.IsValid)
            {
                if (createIndexResult.ServerError != null && string.Equals(createIndexResult.ServerError.ExceptionType, "IndexAlreadyExistsException", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                ReportEsRequestError(createIndexResult, "Create index");
            }
        }

        private string GetIndexName()
        {
            var now = DateTimeOffset.UtcNow;
            var retval = this.indexNamePrefix + now.Year.ToString() + Dot + now.Month.ToString() + Dot + now.Day.ToString();
            return retval;
        }

        private void ReportEsRequestError(IResponse response, string request)
        {
            Debug.Assert(!response.IsValid);

            if (response.ServerError != null)
            {
                DiagnosticChannelEventSource.Current.EsRequestError(this.contextInfo, request, 
                    response.ServerError.Error, response.ServerError.ExceptionType, response.ServerError.Status);
            }
            else
            {
                DiagnosticChannelEventSource.Current.EsRequestError(this.contextInfo, request, "(unknown)", "(unknown)", 0);
            }
        }
    }
}
