// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
namespace Microsoft.Diagnostics.EventFlow.FunctionalTests.HealthReporterBuster
{
    internal class CustomHealthReporter : CsvHealthReporter
    {
        static volatile int count = -1;
        public CustomHealthReporter(CsvHealthReporterConfiguration configuration, INewReportFileTrigger newReportTrigger)
            : base(configuration, newReportTrigger)
        {
        }

        internal override string RotateLogFileImp(string logFileFolder, Func<string, bool> fileExist, Action<string> fileDelete, Action<string, string> fileMove)
        {
            int newCount = Interlocked.Increment(ref count);
            return base.Configuration.LogFilePrefix + newCount.ToString() + ".csv";
        }
    }
}
