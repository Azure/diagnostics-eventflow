// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// -------

using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Microsoft.Diagnostics.EventFlow.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Outputs.ElasticSearch
{
    internal static class ElasticSearchExtensions
    {
        internal enum ElasticConnectionPoolType
        {
            SingleNode,
            Static,
            Sniffing,
            Sticky
        }

        internal static IConnectionPool GetConnectionPool(this ElasticSearchOutputConfiguration elasticConfig, IHealthReporter healthReporter)
        {
            var esServiceUris = elasticConfig.GetEsServiceUriList(healthReporter);
            var esConnectionPool = elasticConfig.GetEsConnectionPoolType(healthReporter);

            switch (esConnectionPool)
            {
                case ElasticConnectionPoolType.SingleNode:
                    return new SingleNodeConnectionPool(esServiceUris.First());
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

        private static ElasticConnectionPoolType GetEsConnectionPoolType(
            this ElasticSearchOutputConfiguration elasticConfig, IHealthReporter healthReporter)
        {
            var enumParseResult =
                Enum.TryParse<ElasticConnectionPoolType>(elasticConfig.ConnectionPoolType, out var elasticConnectionPool);
            if (enumParseResult)
            {
                return elasticConnectionPool;
            }

            //Invalid config string report and throw
            var errorMessage = $"{nameof(ElasticSearchOutput)}:  required 'ConnectionPoolType' configuration parameter is invalid. Using default.";
            healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
            return ElasticConnectionPoolType.SingleNode;
        }

        private static IEnumerable<Uri> GetEsServiceUriList(this ElasticSearchOutputConfiguration elasticConfig, IHealthReporter healthReporter)
        {
            var esServiceUri = elasticConfig.ServiceUri
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