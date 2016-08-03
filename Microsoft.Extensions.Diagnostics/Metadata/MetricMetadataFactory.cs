// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Configuration;
using Validation;

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public static class MetricMetadataFactory
    {
        public static IMetricMetadataCollection CreateMetricMetadata(IConfigurationRoot configurationRoot, IHealthReporter healthReporter)
        {
            Requires.NotNull(configurationRoot, nameof(configurationRoot));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            var innerCollection = new HybridDictionary();

            IConfiguration eventSourcesConfiguration = configurationRoot.GetSection("EventSources");
            if (eventSourcesConfiguration == null)
            {
                healthReporter.ReportProblem("MetricMetadataFactory: required configuration section 'EventSources' missing");
                
            }
            else
            {
                var eventSources = new List<EventSourceConfiguration>();
                eventSourcesConfiguration.Bind(eventSources);
                foreach (EventSourceConfiguration esConfiguration in eventSources)
                {
                    if (esConfiguration.Metrics == null)
                    {
                        continue;
                    }

                    foreach(MetricMetadata metricConfiguration in esConfiguration.Metrics)
                    {
                        innerCollection[MetricMetadataCollection.GetCollectionKey(esConfiguration.ProviderName, metricConfiguration.EventName)] = metricConfiguration;                        
                    }
                }
            }
            
            return new MetricMetadataCollection(innerCollection);
        }
    }    
}
