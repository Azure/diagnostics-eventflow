// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public class PerformanceCounterMetricMetadata : IMetricMetadata
    {
        public string Name { get; set; }
        public string MetricValueProperty { get; set; }
        public double MetricValue { get; set; }
    }
}
