// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Configuration;
using Validation;

#if NET45
using System.Collections.Specialized;
#endif

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public static class EventSourceMetadataFactory
    {
        public static EventMetadataCollection<TMetadata> ReadMetadata<TMetadata>(
            IConfigurationRoot configurationRoot, 
            IHealthReporter healthReporter, 
            Func<EventSourceConfiguration, IEnumerable<TMetadata>> metadataSelector)
            where TMetadata : EventMetadata
        {
            Requires.NotNull(configurationRoot, nameof(configurationRoot));
            Requires.NotNull(healthReporter, nameof(healthReporter));
            Requires.NotNull(metadataSelector, nameof(metadataSelector));

#if NET45
            var innerCollection = new HybridDictionary();
#elif NETSTANDARD1_6
            var innerCollection = new Dictionary<string,object>();
#endif
            
            IConfiguration eventSourcesConfiguration = configurationRoot.GetSection("EventSources");
            if (eventSourcesConfiguration == null)
            {
                healthReporter.ReportProblem("MetadataFactory: required configuration section 'EventSources' missing");
            }
            else
            {
                var eventSources = new List<EventSourceConfiguration>();
                eventSourcesConfiguration.Bind(eventSources);
                foreach (EventSourceConfiguration esConfiguration in eventSources)
                {
                    IEnumerable<TMetadata> metadataEnumerable = metadataSelector(esConfiguration);
                    if (metadataEnumerable == null)
                    {
                        continue;
                    }

                    foreach (TMetadata metadata in metadataEnumerable)
                    {
                        metadata.ProviderName = esConfiguration.ProviderName;
                        if (!metadata.Validate())
                        {
                            healthReporter.ReportProblem($"MetadataFactory: configuration for provider {esConfiguration.ProviderName} event {metadata.EventName} is invalid");
                            continue;
                        }
                        innerCollection[EventMetadata.GetCollectionKey(esConfiguration.ProviderName, metadata.EventName)] = metadata;
                    }
                }
            }

            return new EventMetadataCollection<TMetadata>(innerCollection);
        }
    }
}
