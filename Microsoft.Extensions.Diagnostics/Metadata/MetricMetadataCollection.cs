// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Specialized;
using Validation;

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public interface IMetricMetadataCollection
    {
        MetricMetadata GetMetadata(string providerName, string eventName);
    }

    internal class MetricMetadataCollection : IMetricMetadataCollection
    {
        private HybridDictionary source;

        public MetricMetadataCollection(HybridDictionary source)
        {
            Requires.NotNull(source, nameof(source));
            this.source = source;
        }

        public MetricMetadata GetMetadata(string providerName, string eventName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(eventName))
            {
                return null;
            }

            string key = GetCollectionKey(providerName, eventName);
            return (MetricMetadata) this.source[key];
        }

        public static string GetCollectionKey(string providerName, string eventName)
        {
            return providerName + eventName;
        }
    }
}
