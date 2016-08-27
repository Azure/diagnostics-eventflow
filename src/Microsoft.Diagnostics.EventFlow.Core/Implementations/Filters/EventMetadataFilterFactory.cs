// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Filters
{
    public class EventMetadataFilterFactory: IPipelineItemFactory<EventMetadataFilter>
    {
        public EventMetadataFilter CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            var metadataFilterConfiguration = new EventMetadataFilterConfiguration();
            try
            {
                configuration.Bind(metadataFilterConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"{nameof(EventMetadataFilterFactory)}: configuration is invalid for filter {configuration.ToString()}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(metadataFilterConfiguration.Metadata))
            {
                healthReporter.ReportProblem($"{nameof(EventMetadataFilterFactory)}: metadata type ('metadata' property) must be specified for metadata filter");
                return null;
            }

            var metadata = new EventMetadata(metadataFilterConfiguration.Metadata);

            foreach (var configurationProperty in configuration.AsEnumerable())
            {
                if (configurationProperty.Value == null)
                {
                    // The enumerable includes an item that represents the whole configuration fragment. It's value is null
                    continue;
                }

                // The Key property contains full path, with path elements separated by colons. We need to extract the last path fragment
                int lastColonIndex = configurationProperty.Key.LastIndexOf(':');
                if (lastColonIndex < 0 || lastColonIndex == configurationProperty.Key.Length - 1)
                {
                    continue;
                }
                string propertyName = configurationProperty.Key.Substring(lastColonIndex + 1);

                // Do not store generic metadata filter properties
                if ( nameof(EventMetadataFilterConfiguration.Metadata).Equals(propertyName, StringComparison.OrdinalIgnoreCase)
                  || nameof(ItemConfiguration.Type).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (nameof(EventMetadataFilterConfiguration.Include).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    metadata.IncludeCondition = configurationProperty.Value;
                    continue;
                }

                metadata.Properties[propertyName] = configurationProperty.Value;
            }

            return new EventMetadataFilter(metadata);
        }
    }
}
