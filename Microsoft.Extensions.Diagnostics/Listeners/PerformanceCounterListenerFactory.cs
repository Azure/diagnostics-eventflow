// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public static class PerformanceCounterListenerFactory
    {
        public static PerformanceCounterListener CreateListener(IConfigurationRoot configurationRoot, IHealthReporter healthReporter)
        {
            Requires.NotNull(configurationRoot, nameof(configurationRoot));

            IConfiguration performanceCounterConfiguration = configurationRoot.GetSection("PerformanceCounterMetrics");
            return performanceCounterConfiguration != null ?
                new PerformanceCounterListener(performanceCounterConfiguration, healthReporter) :
                null;
        }
    }
}
