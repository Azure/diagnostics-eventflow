// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nest;
using Validation;

namespace Microsoft.Extensions.Diagnostics.Outputs
{
    public class ElasticSearchOutput : OutputBase
    {
        private const string Dot = ".";
        private const string Dash = "-";

        // TODO: make it a (configuration) property of the listener
        private const string EventDocumentTypeName = "event";

        private ElasticSearchConnectionData connectionData;
        // TODO: support for multiple ES nodes/connection pools, for failover and load-balancing        

        public ElasticSearchOutput(IConfiguration configuration, IHealthReporter healthReporter) : base(healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.connectionData = CreateConnectionData(configuration, healthReporter);
        }        

        public override async Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (this.connectionData == null || events == null || events.Count == 0)
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

                this.ReportHealthy();
            }
            catch (Exception e)
            {
                this.ReportProblem($"{nameof(ElasticSearchOutput)}: diagnostics data upload has failed.{Environment.NewLine}{e.ToString()}");
            }
        }

        private ElasticClient CreateElasticClient(IConfiguration configuration, IHealthReporter healthReporter)
        {
            string esServiceUriString = configuration["serviceUri"];
            Uri esServiceUri;
            bool serviceUriIsValid = Uri.TryCreate(esServiceUriString, UriKind.Absolute, out esServiceUri);
            if (!serviceUriIsValid)
            {
                healthReporter.ReportProblem($"{nameof(ElasticSearchOutput)}:  configuration is missing required 'serviceUri' parameter");
                return null;
            }

            string userName = configuration["userName"];
            string password = configuration["password"];
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                healthReporter.ReportProblem($"{nameof(ElasticSearchOutput)}:  configuration is missing Elastic Search credentials");
                return null;
            }

            ConnectionSettings config = new ConnectionSettings(esServiceUri).BasicAuthentication(userName, password);
            return new ElasticClient(config);
        }

        private ElasticSearchConnectionData CreateConnectionData(IConfiguration configuration, IHealthReporter healthReporter)
        {
            var connectionData = new ElasticSearchConnectionData();
            var client = this.CreateElasticClient(configuration, healthReporter);
            if (client == null)
            {
                return null;
            }
            connectionData.Client = client;
            connectionData.LastIndexName = null;
            string indexNamePrefix = configuration["indexNamePrefix"];
            connectionData.IndexNamePrefix = string.IsNullOrWhiteSpace(indexNamePrefix) ? string.Empty : indexNamePrefix + Dash;
            return connectionData;
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
            IndexState indexSettings = new IndexState();
            indexSettings.Settings = new IndexSettings();
            indexSettings.Settings.NumberOfReplicas = 1;
            indexSettings.Settings.NumberOfShards = 5;
            indexSettings.Settings.Add("refresh_interval", "15s");

            ICreateIndexResponse createIndexResult = await esClient.CreateIndexAsync(currentIndexName, c => c.InitializeUsing(indexSettings));

            if (!createIndexResult.IsValid)
            {
                if (createIndexResult.ServerError != null &&
                    createIndexResult.ServerError.Error != null &&
                    string.Equals(createIndexResult.ServerError.Error.Type, "IndexAlreadyExistsException", StringComparison.OrdinalIgnoreCase))
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

            string errorMessage = $"{nameof(ElasticSearchOutput)}: request resulted in an error: ";

            if (response.ServerError != null)
            {
                errorMessage += $"{response.ServerError.Error}{Environment.NewLine}" +
                                $"ExceptionType: {response.ServerError.Error.Type}{Environment.NewLine}" +
                                $"Status code: {response.ServerError.Status}";
            }
            else if (response.DebugInformation != null)
            {
                errorMessage += $"Debug information: {response.DebugInformation}";
            }
            else
            {
                // Hopefully never happens
                errorMessage += "No further error information is available";
            }

            this.ReportProblem(errorMessage);
        }

        private class ElasticSearchConnectionData
        {
            public ElasticClient Client { get; set; }

            public string IndexNamePrefix { get; set; }

            public string LastIndexName { get; set; }
        }
    }
}