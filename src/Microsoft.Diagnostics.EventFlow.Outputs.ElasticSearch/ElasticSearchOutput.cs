// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Extensions.Configuration;
using Nest;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    public class ElasticSearchOutput : IOutput
    {
        private const string Dot = ".";
        private const string Dash = "-";

        private ElasticSearchConnectionData connectionData;
        // TODO: support for multiple ES nodes/connection pools, for failover and load-balancing        

        private readonly IHealthReporter healthReporter;

        public ElasticSearchOutput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            var esOutputConfiguration = new ElasticSearchOutputConfiguration();
            try
            {
                configuration.Bind(esOutputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(ElasticSearchOutput)} configuration encountered: '{configuration.ToString()}'",
                    EventFlowContextIdentifiers.Configuration);
                throw;
            }

            Initialize(esOutputConfiguration);
        }

        public ElasticSearchOutput(ElasticSearchOutputConfiguration elasticSearchOutputConfiguration, IHealthReporter healthReporter)
        {
            Requires.NotNull(elasticSearchOutputConfiguration, nameof(elasticSearchOutputConfiguration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;

            // Clone the configuration instance since we are going to hold onto it (via this.connectionData)
            Initialize(elasticSearchOutputConfiguration.DeepClone());
        }

        public async Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
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
                    await this.EnsureIndexExists(currentIndexName, this.connectionData.Client).ConfigureAwait(false);
                    this.connectionData.LastIndexName = currentIndexName;
                }

                BulkRequest request = new BulkRequest();
                request.Refresh = true;

                List<IBulkOperation> operations = new List<IBulkOperation>();
                string documentTypeName = this.connectionData.Configuration.EventDocumentTypeName;
                foreach (EventData eventData in events)
                {
                    BulkCreateOperation<EventData> operation = new BulkCreateOperation<EventData>(eventData);
                    operation.Index = currentIndexName;
                    operation.Type = documentTypeName;
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
                IBulkResponse response = await this.connectionData.Client.BulkAsync(request).ConfigureAwait(false);
                if (!response.IsValid)
                {
                    this.ReportEsRequestError(response, "Bulk upload");
                }
                else
                {
                    this.healthReporter.ReportHealthy();
                }
            }
            catch (Exception e)
            {
                string errorMessage = nameof(ElasticSearchOutput) + ": diagnostics data upload has failed." + Environment.NewLine + e.ToString();
                this.healthReporter.ReportProblem(errorMessage);
            }
        }

        private void Initialize(ElasticSearchOutputConfiguration esOutputConfiguration)
        {
            Debug.Assert(esOutputConfiguration != null);
            Debug.Assert(this.healthReporter != null);

            this.connectionData = new ElasticSearchConnectionData();
            this.connectionData.Configuration = esOutputConfiguration;

            Uri esServiceUri;
            string errorMessage;

            bool serviceUriIsValid = Uri.TryCreate(esOutputConfiguration.ServiceUri, UriKind.Absolute, out esServiceUri);
            if (!serviceUriIsValid)
            {
                errorMessage = $"{nameof(ElasticSearchOutput)}:  required 'serviceUri' configuration parameter is invalid";
                this.healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }

            string userName = esOutputConfiguration.BasicAuthenticationUserName;
            string password = esOutputConfiguration.BasicAuthenticationUserPassword;
            bool credentialsIncomplete = string.IsNullOrWhiteSpace(userName) ^ string.IsNullOrWhiteSpace(password);
            if (credentialsIncomplete)
            {
                errorMessage = $"{nameof(ElasticSearchOutput)}: for basic authentication to work both user name and password must be specified";
                healthReporter.ReportWarning(errorMessage, EventFlowContextIdentifiers.Configuration);
                userName = password = null;
            }

            ConnectionSettings connectionSettings = new ConnectionSettings(esServiceUri);
            if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
            { 
                connectionSettings = connectionSettings.BasicAuthentication(userName, password);
            }

            this.connectionData.Client = new ElasticClient(connectionSettings);
            this.connectionData.LastIndexName = null;

            if (string.IsNullOrWhiteSpace(esOutputConfiguration.IndexNamePrefix))
            {
                esOutputConfiguration.IndexNamePrefix = string.Empty;
            }
            else
            {
                string lowerCaseIndexNamePrefix = esOutputConfiguration.IndexNamePrefix.ToLowerInvariant();
                if (lowerCaseIndexNamePrefix != esOutputConfiguration.IndexNamePrefix)
                {
                    healthReporter.ReportWarning($"{nameof(ElasticSearchOutput)}: The chosen index name prefix '{esOutputConfiguration.IndexNamePrefix}' "
                                                + "contains uppercase characters, which are not allowed by Elasticsearch. The prefix will be converted to lowercase.",
                                                EventFlowContextIdentifiers.Configuration);
                }
                esOutputConfiguration.IndexNamePrefix = lowerCaseIndexNamePrefix + Dash;
            }

            if (string.IsNullOrWhiteSpace(esOutputConfiguration.EventDocumentTypeName))
            {
                string warning = $"{nameof(ElasticSearchOutput)}: '{nameof(ElasticSearchOutputConfiguration.EventDocumentTypeName)}' configuration parameter "
                                + "should not be empty";
                healthReporter.ReportWarning(warning, EventFlowContextIdentifiers.Configuration);
                esOutputConfiguration.EventDocumentTypeName = ElasticSearchOutputConfiguration.DefaultEventDocumentTypeName;
            }
        }

        private async Task EnsureIndexExists(string indexName, ElasticClient esClient)
        {
            IExistsResponse existsResult = await esClient.IndexExistsAsync(indexName).ConfigureAwait(false);
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

            ICreateIndexResponse createIndexResult = await esClient.CreateIndexAsync(indexName, c => c.InitializeUsing(indexSettings)).ConfigureAwait(false);

            if (!createIndexResult.IsValid)
            {
                try
                {
                    if (createIndexResult.ServerError?.Error?.Type != null &&
                    Regex.IsMatch(createIndexResult.ServerError.Error.Type, "index.*already.*exists.*exception", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500)))
                    {
                        // This is fine, someone just beat us to create a new index.
                        return;
                    }
                }
                catch (RegexMatchTimeoutException) { }

                this.ReportEsRequestError(createIndexResult, "Create index");
            }
        }

        private string GetIndexName(ElasticSearchConnectionData connectionData)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string retval = connectionData.Configuration.IndexNamePrefix + now.Year.ToString() + Dot + now.Month.ToString() + Dot + now.Day.ToString();
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

            this.healthReporter.ReportProblem(errorMessage);
        }

        private class ElasticSearchConnectionData
        {
            public ElasticClient Client { get; set; }

            public ElasticSearchOutputConfiguration Configuration { get; set; }

            public string LastIndexName { get; set; }
        }
    }
}