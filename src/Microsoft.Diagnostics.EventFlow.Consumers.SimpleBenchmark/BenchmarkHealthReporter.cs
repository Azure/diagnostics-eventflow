// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading;

namespace Microsoft.Diagnostics.EventFlow.Consumers.SimpleBenchmark
{
    internal class BenchmarkHealthReporter : IHealthReporter
    {
        private long healthyCount = 0;
        private long warningCount = 0;
        private long problemCount = 0;

        public string Summary
        {
            get { return $"Health reports received: {healthyCount} healthy, {warningCount} warnings, {problemCount} problems"; }
        }

        public void Dispose()
        {
        }

        public void ReportHealthy(string description = null, string context = null)
        {
            Interlocked.Increment(ref healthyCount);
        }

        public void ReportProblem(string description, string context = null)
        {
            Interlocked.Increment(ref problemCount);
        }

        public void ReportWarning(string description, string context = null)
        {
            Interlocked.Increment(ref warningCount);
        }

        public void ResetCounters()
        {
            this.healthyCount = 0;
            this.warningCount = 0;
            this.problemCount = 0;
        }
    }
}
