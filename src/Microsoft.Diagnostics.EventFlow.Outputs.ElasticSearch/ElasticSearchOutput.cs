// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Microsoft.Diagnostics.EventFlow.Utilities;
using Microsoft.Extensions.Configuration;
using Nest;
using Validation;
using RequestData = Microsoft.Diagnostics.EventFlow.Metadata.RequestData;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    public class ElasticSearchOutput : IOutput
    {
        private const string Dot = ".";
        private const string Dash = "-";

        private ElasticSearchConnectionData connectionData;

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

                List<IBulkOperation> operations = new List<IBulkOperation>();
                string documentTypeName = this.connectionData.Configuration.EventDocumentTypeName;

                foreach (EventData eventData in events)
                {
                    operations.AddRange(GetCreateOperationsForEvent(eventData, currentIndexName, documentTypeName));
                }

                request.Operations = operations;

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Note: the NEST client is documented to be thread-safe so it should be OK to just reuse the this.esClient instance
                // between different SendEventsAsync callbacks.
                // Reference: https://www.elastic.co/blog/nest-and-elasticsearch-net-1-3
                BulkResponse response = await this.connectionData.Client.BulkAsync(request).ConfigureAwait(false);
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
                ErrorHandlingPolicies.HandleOutputTaskError(e, () => 
                {
                    string errorMessage = nameof(ElasticSearchOutput) + ": diagnostics data upload has failed." + Environment.NewLine + e.ToString();
                    this.healthReporter.ReportWarning(errorMessage, EventFlowContextIdentifiers.Output);
                });
            }
        }

        private IEnumerable<IBulkOperation> GetCreateOperationsForEvent(EventData eventData, string currentIndexName, string documentTypeName)
        {
            bool reportedAsSpecialEvent = false;
            BulkIndexOperation<EventData> operation;
            IReadOnlyCollection<EventMetadata> metadataSet;

            // Synthesize a separate record for each metric, request, dependency and exception metadata associated with the event

            if (eventData.TryGetMetadata(MetricData.MetricMetadataKind, out metadataSet))
            {
                foreach (var metricMetadata in metadataSet)
                {
                    operation = CreateMetricOperation(eventData, metricMetadata, currentIndexName);
                    if (operation != null)
                    {
                        reportedAsSpecialEvent = true;
                        yield return operation;
                    }                    
                }
            }

            if (eventData.TryGetMetadata(RequestData.RequestMetadataKind, out metadataSet))
            {
                foreach (var requestMetadata in metadataSet)
                {
                    operation = CreateRequestOperation(eventData, requestMetadata, currentIndexName);
                    if (operation != null)
                    {
                        reportedAsSpecialEvent = true;
                        yield return operation;
                    }
                }
            }

            if (eventData.TryGetMetadata(DependencyData.DependencyMetadataKind, out metadataSet))
            {
                foreach (var dependencyMetadata in metadataSet)
                {
                    operation = CreateDependencyOperation(eventData, dependencyMetadata, currentIndexName);
                    if (operation != null)
                    {
                        reportedAsSpecialEvent = true;
                        yield return operation;
                    }
                }
            }

            if (eventData.TryGetMetadata(DependencyData.DependencyMetadataKind, out metadataSet))
            {
                foreach (var dependencyMetadata in metadataSet)
                {
                    operation = CreateDependencyOperation(eventData, dependencyMetadata, currentIndexName);
                    if (operation != null)
                    {
                        reportedAsSpecialEvent = true;
                        yield return operation;
                    }
                }
            }

            if (eventData.TryGetMetadata(ExceptionData.ExceptionMetadataKind, out metadataSet))
            {
                foreach (var exceptionMetadata in metadataSet)
                {
                    operation = CreateExceptionOperation(eventData, exceptionMetadata, currentIndexName);
                    if (operation != null)
                    {
                        reportedAsSpecialEvent = true;
                        yield return operation;
                    }
                }
            }

            if (!reportedAsSpecialEvent)
            {
                operation = CreateOperation(eventData, currentIndexName);
                yield return operation;
            }
        }

        private BulkIndexOperation<EventData> CreateMetricOperation(EventData eventData, EventMetadata metricMetadata, string currentIndexName)
        {
            var result = MetricData.TryGetData(eventData, metricMetadata, out MetricData metricData);
            if (result.Status != DataRetrievalStatus.Success)
            {
                this.healthReporter.ReportProblem("ElasticSearchOutput: " + result.Message, EventFlowContextIdentifiers.Output);
                return null;
            }

            var metricEventData = eventData.DeepClone();
            metricEventData.Payload[nameof(MetricData.MetricName)] = metricData.MetricName;
            metricEventData.Payload[nameof(MetricData.Value)] = metricData.Value;
            var operation = CreateOperation(metricEventData, currentIndexName);
            return operation;
        }

        private BulkIndexOperation<EventData> CreateRequestOperation(EventData eventData, EventMetadata requestMetadata, string currentIndexName)
        {
            var result = RequestData.TryGetData(eventData, requestMetadata, out RequestData requestData);
            if (result.Status != DataRetrievalStatus.Success)
            {
                this.healthReporter.ReportProblem("ElasticSearchOutput: " + result.Message, EventFlowContextIdentifiers.Output);
                return null;
            }

            var requestEventData = eventData.DeepClone();
            requestEventData.Payload[nameof(RequestData.RequestName)] = requestData.RequestName;
            if (requestData.Duration != null)
            {
                requestEventData.Payload[nameof(RequestData.Duration)] = requestData.Duration;
            }
            if (requestData.IsSuccess != null)
            {
                requestEventData.Payload[nameof(RequestData.IsSuccess)] = requestData.IsSuccess;
            }
            if (requestData.ResponseCode != null)
            {
                requestEventData.Payload[nameof(RequestData.ResponseCode)] = requestData.ResponseCode;
            }
            var operation = CreateOperation(requestEventData, currentIndexName);
            return operation;
        }

        private BulkIndexOperation<EventData> CreateDependencyOperation(EventData eventData, EventMetadata dependencyMetadata, string currentIndexName)
        {
            var result = DependencyData.TryGetData(eventData, dependencyMetadata, out DependencyData dependencyData);
            if (result.Status != DataRetrievalStatus.Success)
            {
                this.healthReporter.ReportProblem("ElasticSearchOutput: " + result.Message, EventFlowContextIdentifiers.Output);
                return null;
            }

            var dependencyEventData = eventData.DeepClone();
            if (dependencyData.Duration != null)
            {
                dependencyEventData.Payload[nameof(DependencyData.Duration)] = dependencyData.Duration;
            }
            if (dependencyData.IsSuccess != null)
            {
                dependencyEventData.Payload[nameof(DependencyData.IsSuccess)] = dependencyData.IsSuccess;
            }
            if (dependencyData.ResponseCode != null)
            {
                dependencyEventData.Payload[nameof(DependencyData.ResponseCode)] = dependencyData.ResponseCode;
            }
            if (dependencyData.Target != null)
            {
                dependencyEventData.Payload[nameof(DependencyData.Target)] = dependencyData.Target;
            }
            if (dependencyData.DependencyType != null)
            {
                dependencyEventData.Payload[nameof(DependencyData.DependencyType)] = dependencyData.DependencyType;
            }
            var operation = CreateOperation(dependencyEventData, currentIndexName);
            return operation;
        }

        private BulkIndexOperation<EventData> CreateExceptionOperation(EventData eventData, EventMetadata exceptionMetadata, string currentIndexName)
        {
            var result = ExceptionData.TryGetData(eventData, exceptionMetadata, out ExceptionData exceptionData);
            if (result.Status != DataRetrievalStatus.Success)
            {
                this.healthReporter.ReportProblem("ElasticSearchOutput: " + result.Message, EventFlowContextIdentifiers.Output);
                return null;
            }

            var exceptionEventData = eventData.DeepClone();
            exceptionEventData.Payload[nameof(ExceptionData.Exception)] = exceptionData.Exception.ToString();
            var operation = CreateOperation(exceptionEventData, currentIndexName);
            return operation;
        }

        private static BulkIndexOperation<EventData> CreateOperation(EventData eventData, string currentIndexName)
        {
            BulkIndexOperation<EventData> operation = new BulkIndexOperation<EventData>(eventData);            
            operation.Index = currentIndexName;
            return operation;
        }

        private void Initialize(ElasticSearchOutputConfiguration esOutputConfiguration)
        {
            Debug.Assert(esOutputConfiguration != null);
            Debug.Assert(this.healthReporter != null);

            this.connectionData = new ElasticSearchConnectionData
            {
                Configuration = esOutputConfiguration
            };

            string userName = esOutputConfiguration.BasicAuthenticationUserName;
            string password = esOutputConfiguration.BasicAuthenticationUserPassword;
            bool credentialsIncomplete = string.IsNullOrWhiteSpace(userName) ^ string.IsNullOrWhiteSpace(password);
            if (credentialsIncomplete)
            {
                var errorMessage = $"{nameof(ElasticSearchOutput)}: for basic authentication to work both user name and password must be specified";
                healthReporter.ReportWarning(errorMessage, EventFlowContextIdentifiers.Configuration);
                userName = password = null;
            }

            IConnectionPool pool = esOutputConfiguration.GetConnectionPool(healthReporter);
            ConnectionSettings connectionSettings = new ConnectionSettings(pool);
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
            ExistsResponse existsResult = await esClient.Indices.ExistsAsync(indexName).ConfigureAwait(false);
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
            indexSettings.Settings.NumberOfReplicas = this.connectionData.Configuration.NumberOfReplicas;
            indexSettings.Settings.NumberOfShards = this.connectionData.Configuration.NumberOfShards;
            indexSettings.Settings.Add("refresh_interval", this.connectionData.Configuration.RefreshInterval);
            if (this.connectionData.Configuration.DefaultPipeline != null)
            {
                indexSettings.Settings.Add("default_pipeline", this.connectionData.Configuration.DefaultPipeline);
            }

            CreateIndexResponse createIndexResult = await esClient.Indices.CreateAsync(indexName, c => c.InitializeUsing(indexSettings)).ConfigureAwait(false);

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
            string retval = connectionData.Configuration.IndexNamePrefix + now.ToString("yyyy" + Dot + "MM" + Dot + "dd");
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

            this.healthReporter.ReportWarning(errorMessage);
        }

        private class ElasticSearchConnectionData
        {
            public ElasticClient Client { get; set; }

            public ElasticSearchOutputConfiguration Configuration { get; set; }

            public string LastIndexName { get; set; }
        }
    }
}