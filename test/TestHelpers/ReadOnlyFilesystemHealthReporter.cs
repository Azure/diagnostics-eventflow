// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.TestHelpers
{
    public class ReadOnlyFilesystemHealthReporter: CsvHealthReporter
    {
        public bool LogRotationAttempted { get; private set; }
        public int HealthyReportCount { get; private set; }

        public ReadOnlyFilesystemHealthReporter(IConfiguration configuration) 
            : base(configuration.ToCsvHealthReporterConfiguration())
        {
            LogRotationAttempted = false;
        }

        internal override string RotateLogFileImp(string logFileFolder, Func<string, bool> fileExist, Action<string> fileDelete, Action<string, string> fileMove)
        {
            LogRotationAttempted = true;
            // See https://github.com/Azure/diagnostics-eventflow/issues/255 for how this may happen
            throw new IOException("File system is read only");
        }

        public override void ReportHealthy(string description = null, string context = null)
        {
            base.ReportHealthy(description, context);
            HealthyReportCount++;
        }
    }
}
