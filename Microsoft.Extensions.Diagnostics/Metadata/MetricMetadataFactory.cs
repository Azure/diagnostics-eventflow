// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public static class MetricMetadataFactory
    {
        public static IReadOnlyDictionary<MetricLookupKey, MetricMetadata> CreateMetricMetadata(IConfigurationRoot configurationRoot, IHealthReporter healthReporter)
        {
            Requires.NotNull(configurationRoot, nameof(configurationRoot));

            IConfiguration eventSourceConfiguration = configurationRoot.GetSection("EventSources");
        }
    }

    public class MetricLookupKey
    {
        public MetricLookupKey(string providerName, string eventName)
        {
            Requires.NotNull(providerName, nameof(providerName));
            Requires.NotNull(eventName, nameof(eventName));

            ProviderName = providerName;
            EventName = eventName;
        }

        public string ProviderName { get; private set; }
        public string EventName { get; private set; }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return ProviderName.GetHashCode() ^ EventName.GetHashCode();
        }
    }
}
