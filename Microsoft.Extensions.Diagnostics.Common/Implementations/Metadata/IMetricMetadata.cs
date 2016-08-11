// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public interface IMetricMetadata
    {
        string MetricName { get; set; }
        string MetricValueProperty { get; set; }
        double MetricValue { get; set; }
    }
}
