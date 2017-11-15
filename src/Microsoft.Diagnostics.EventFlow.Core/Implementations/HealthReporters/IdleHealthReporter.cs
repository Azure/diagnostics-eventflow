// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.HealthReporters
{
    /// <summary>
    /// Health report that does nothing.
    /// </summary>
    internal class IdleHealthReporter : IHealthReporter
    {
        public void Dispose()
        {
        }

        public void ReportHealthy(string description = null, string context = null)
        {
        }

        public void ReportProblem(string description, string context = null)
        {
        }

        public void ReportWarning(string description, string context = null)
        {
        }
    }
}
