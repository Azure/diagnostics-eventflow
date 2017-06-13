// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Microsoft.Diagnostics.EventFlow
{
    internal static class HealthReporterExtensions
    {
        public static void ReportThrottling(this IHealthReporter healthReporter)
        {
            healthReporter.ReportWarning("An event was dropped from the diagnostic pipeline because there was not enough capacity",
                    EventFlowContextIdentifiers.Throttling);
        }
    }
}
