// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public class MetricMetadata: EventMetadata
    {
        public string Name { get; set; }    // Metric name
        public string MetricValueProperty { get; set; }
        public double MetricValue { get; set; }

        public override bool Validate()
        {
            return base.Validate() && !string.IsNullOrWhiteSpace(Name);
        }
    }
}
