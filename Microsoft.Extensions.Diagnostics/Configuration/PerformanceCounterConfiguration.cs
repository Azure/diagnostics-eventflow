// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    public class PerformanceCounterConfiguration
    {
        public string MetricName { get; set; }
        public string CounterCategory { get; set; }
        public string CounterName { get; set; }
        public int CollectionIntervalInSeconds { get; set; }

        public virtual bool Validate()
        {
            return !string.IsNullOrWhiteSpace(MetricName) && !string.IsNullOrWhiteSpace(CounterCategory) && !string.IsNullOrWhiteSpace(CounterName);
        }
    }
}
