// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventListeners
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Nest;

    public class ElasticSearchListener : BufferingEventListener, IDisposable
    {
        private const string Dot = ".";
        private const string Dash = "-";
        // TODO: make it a (configuration) property of the listener
        private const string EventDocumentTypeName = "event";
        private ElasticSearchConnectionData connectionData;
        // TODO: support for multiple ES nodes/connection pools, for failover and load-balancing        

        public ElasticSearchListener(IConfigurationProvider configurationProvider, IHealthReporter healthReporter) : base(configurationProvider, healthReporter)
        {
            if (this.Disabled)
            {
                return;
            }

            Debug.Assert(configurationProvider != null);
            this.CreateConnectionData(configurationProvider);

            this.Sender = new ConcurrentEventSender<EventData>(
                eventBufferSize: 1000,
                maxConcurrency: 2,
                batchSize: 100,
                noEventsDelay: TimeSpan.FromMilliseconds(1000),
                transmitterProc: this.SendEventsAsync,
                healthReporter: healthReporter);
        }

        private ElasticClient CreateElasticClient(IConfigurationProvider configurationProvider)
        {
            string esServiceUriString = configurationProvider.GetValue("serviceUri");
            Uri esServiceUri;
            bool serviceUriIsValid = Uri.TryCreate(esServiceUriString, UriKind.Absolute, out esServiceUri);
            if (!serviceUriIsValid)
            {
                throw new ConfigurationErrorsException("serviceUri must be a valid, absolute URI");
            }

            string userName = configurationProvider.GetValue("userName");
            string password = configurationProvider.GetValue("password");
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                throw new ConfigurationErrorsException("Invalid Elastic Search credentials");
            }

            ConnectionSettings config = new ConnectionSettings(esServiceUri).SetBasicAuthentication(userName, password);
            return new ElasticClient(config);
        }

        private void CreateConnectionData(object sender)
        {
            IConfigurationProvider configurationProvider = (IConfigurationProvider) sender;

            this.connectionData = new ElasticSearchConnectionData();
            this.connectionData.Client = this.CreateElasticClient(configurationProvider);
            this.connectionData.LastIndexName = null;
            string indexNamePrefix = configurationProvider.GetValue("indexNamePrefix");
            this.connectionData.IndexNamePrefix = string.IsNullOrWhiteSpace(indexNamePrefix) ? string.Empty : indexNamePrefix + Dash;
        }

        private async Task SendEventsAsync(IEnumerable<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (events == null)
            {
                return;
            }

            try
            {
                string currentIndexName = this.GetIndexName(this.connectionData);
                if (!string.Equals(currentIndexName, this.connectionData.LastIndexName, StringComparison.Ordinal))
                {
                    await this.EnsureIndexExists(currentIndexName, this.connectionData.Client);
                    this.connectionData.LastIndexName = currentIndexName;
                }

                BulkRequest request = new BulkRequest();
                request.Refresh = true;

                List<IBulkOperation> operations = new List<IBulkOperation>();
                foreach (EventData eventData in events)
                {
                    BulkCreateOperation<EventData> operation = new BulkCreateOperation<EventData>(eventData);
                    operation.Index = currentIndexName;
                    operation.Type = EventDocumentTypeName;
                    operations.Add(operation);
                }

                request.Operations = operations;

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Note: the NEST client is documented to be thread-safe so it should be OK to just reuse the this.esClient instance
                // between different SendEventsAsync callbacks.
                // Reference: https://www.elastic.co/blog/nest-and-elasticsearch-net-1-3
                IBulkResponse response = await this.connectionData.Client.BulkAsync(request);
                if (!response.IsValid)
                {
                    this.ReportEsRequestError(response, "Bulk upload");
                }

                this.ReportListenerHealthy();
            }
            catch (Exception e)
            {
                this.ReportListenerProblem("Diagnostics data upload has failed." + Environment.NewLine + e.ToString());
            }
        }

        private async Task EnsureIndexExists(string currentIndexName, ElasticClient esClient)
        {
            IExistsResponse existsResult = await esClient.IndexExistsAsync(currentIndexName);
            if (!existsResult.IsValid)
            {
                this.ReportEsRequestError(existsResult, "Index exists check");
            }

            if (existsResult.Exists)
            {
                return;
            }

            // TODO: allow the consumer to fine-tune index settings
            IndexSettings indexSettings = new IndexSettings();
            indexSettings.NumberOfReplicas = 1;
            indexSettings.NumberOfShards = 5;
            indexSettings.Settings.Add("refresh_interval", "15s");

            IIndicesOperationResponse createIndexResult = await esClient.CreateIndexAsync(c => c.Index(currentIndexName).InitializeUsing(indexSettings));

            if (!createIndexResult.IsValid)
            {
                if (createIndexResult.ServerError != null &&
                    string.Equals(createIndexResult.ServerError.ExceptionType, "IndexAlreadyExistsException", StringComparison.OrdinalIgnoreCase))
                {
                    // This is fine, someone just beat us to create a new index.
                    return;
                }

                this.ReportEsRequestError(createIndexResult, "Create index");
            }
        }

        private string GetIndexName(ElasticSearchConnectionData connectionData)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string retval = connectionData.IndexNamePrefix + now.Year.ToString() + Dot + now.Month.ToString() + Dot + now.Day.ToString();
            return retval;
        }

        private void ReportEsRequestError(IResponse response, string request)
        {
            Debug.Assert(!response.IsValid);

            if (response.ServerError != null)
            {
                this.ReportListenerProblem(
                    string.Format(
                        "ElasticSearch communication attempt resulted in an error: {0} \n ExceptionType: {1} \n Status code: {2}",
                        response.ServerError.Error,
                        response.ServerError.ExceptionType,
                        response.ServerError.Status));
            }
            else if (response.ConnectionStatus != null)
            {
                this.ReportListenerProblem(
                    "ElasticSearch communication attempt resulted in an error. Connection status: " + response.ConnectionStatus.ToString());
            }
            else
            {
                // Hopefully never happens
                this.ReportListenerProblem("ElasticSearch communication attempt resulted in an error. No further error information is available");
            }
        }

        private class ElasticSearchConnectionData
        {
            public ElasticClient Client { get; set; }

            public string IndexNamePrefix { get; set; }

            public string LastIndexName { get; set; }
        }
    }
}