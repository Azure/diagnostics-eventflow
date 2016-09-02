// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading;
using Microsoft.Diagnostics.EventFlow.Core.Implementations.HealthReporters;
using Microsoft.Diagnostics.EventFlow.HealthReporters;

namespace Microsoft.Diagnostics.EventFlow.FunctionalTests.HealthReporterBuster
{
    internal class CustomHealthReporter : CsvHealthReporter
    {
        static volatile int count = -1;
        public CustomHealthReporter(CsvHealthReporterConfiguration configuration, INewReportTrigger newReportTrigger)
            : base(configuration, newReportTrigger)
        {
        }

        public override string GetReportFileName()
        {
            Interlocked.Add(ref count, 1);
            return base.Configuration.LogFilePrefix + count.ToString() + ".csv";
        }
    }
}
