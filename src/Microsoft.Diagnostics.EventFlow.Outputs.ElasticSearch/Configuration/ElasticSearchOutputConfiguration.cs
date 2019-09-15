// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Microsoft.Diagnostics.EventFlow.Outputs;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class ElasticSearchOutputConfiguration: ItemConfiguration
    {
        public static readonly string DefaultEventDocumentTypeName = "event";
        public static readonly int DefaultNumberOfShards = 1;
        public static readonly int DefaultNumberOfReplicas = 5;
        public static readonly string DefaultRefreshInterval = "15s";
        public static readonly ElasticConnectionPoolType DefaultConnectionPoolType = ElasticConnectionPoolType.Static; 

        public string IndexNamePrefix { get; set; }
        public string ServiceUri { get; set; }
        public string ConnectionPoolType { get; set; }
        public string BasicAuthenticationUserName { get; set; }
        public string BasicAuthenticationUserPassword { get; set; }
        public string EventDocumentTypeName { get; set; }
        public int NumberOfShards { get; set; }
        public int NumberOfReplicas { get; set; }
        public string RefreshInterval { get; set; }
        public string DefaultPipeline { get; set; }

        public ElasticSearchMappingsConfiguration Mappings { get; set; }

        public ElasticSearchOutputConfiguration()
        {
            EventDocumentTypeName = DefaultEventDocumentTypeName;
            NumberOfShards = DefaultNumberOfShards;
            NumberOfReplicas = DefaultNumberOfReplicas;
            RefreshInterval = DefaultRefreshInterval;
            Mappings = new ElasticSearchMappingsConfiguration();
        }

        public ElasticSearchOutputConfiguration DeepClone()
        {
            var other = new ElasticSearchOutputConfiguration()
            {
                IndexNamePrefix = this.IndexNamePrefix,
                ServiceUri = this.ServiceUri,
                ConnectionPoolType = this.ConnectionPoolType,
                BasicAuthenticationUserName = this.BasicAuthenticationUserName,
                BasicAuthenticationUserPassword = this.BasicAuthenticationUserPassword,
                EventDocumentTypeName = this.EventDocumentTypeName,
                NumberOfShards = this.NumberOfShards,
                NumberOfReplicas = this.NumberOfReplicas,
                RefreshInterval = this.RefreshInterval,
                DefaultPipeline = this.DefaultPipeline,
                Mappings = this.Mappings.DeepClone()
            };

            return other;
        }

        public IConnectionPool GetConnectionPool(IHealthReporter healthReporter)
        {
            var esServiceUris = GetEsServiceUriList(healthReporter);
            var esConnectionPool = GetEsConnectionPoolType(healthReporter);

            switch (esConnectionPool)
            {
                case ElasticConnectionPoolType.Static:
                    return new StaticConnectionPool(esServiceUris);
                case ElasticConnectionPoolType.Sniffing:
                    return new SniffingConnectionPool(esServiceUris);
                case ElasticConnectionPoolType.Sticky:
                    return new StickyConnectionPool(esServiceUris);
                default:
                    throw new ArgumentOutOfRangeException($"Invalid ElasticConnectionPoolType: {esConnectionPool}");
            }
        }

        public enum ElasticConnectionPoolType
        {
            Static,
            Sniffing,
            Sticky
        }

        private ElasticConnectionPoolType GetEsConnectionPoolType(IHealthReporter healthReporter)
        {
            if (ConnectionPoolType != default(string) &&
                Enum.TryParse<ElasticConnectionPoolType>(ConnectionPoolType, out var elasticConnectionPool))
            {
                return elasticConnectionPool;
            }

            var infoMessage = $"{nameof(ElasticSearchOutput)}: Using default {DefaultConnectionPoolType} connection type.";
            healthReporter.ReportHealthy(infoMessage, EventFlowContextIdentifiers.Configuration);
            return DefaultConnectionPoolType;
        }

        private IEnumerable<Uri> GetEsServiceUriList(IHealthReporter healthReporter)
        {
            var esServiceUri = ServiceUri
                .Split(';')
                .Where(x => Uri.IsWellFormedUriString(x, UriKind.Absolute))
                .Select(x => new Uri(x))
                .ToList();

            if (!esServiceUri.Any())
            {
                //Invalid config string report and throw
                var errorMessage = $"{nameof(ElasticSearchOutput)}:  required 'serviceUri' configuration parameter is invalid";
                healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }
            return esServiceUri;
        }
    }
}
