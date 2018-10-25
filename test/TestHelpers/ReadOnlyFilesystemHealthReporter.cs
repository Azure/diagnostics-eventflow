// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.TestHelpers
{
    public class ReadOnlyFilesystemHealthReporter: CsvHealthReporter
    {
        public AutoResetEvent LogRotationAttempted { get; private set; }

        public ReadOnlyFilesystemHealthReporter(IConfiguration configuration) 
            : base(configuration.ToCsvHealthReporterConfiguration())
        {
            this.LogRotationAttempted = new AutoResetEvent(false);
        }

        internal override string RotateLogFileImp(string logFileFolder, Func<string, bool> fileExist, Action<string> fileDelete, Action<string, string> fileMove)
        {
            // Delay the notification so that the exception can bubble up the stack and the test does not end prematurely.
            Task.Delay(TimeSpan.FromMilliseconds(200)).ContinueWith((t) => this.LogRotationAttempted.Set());

            // See https://github.com/Azure/diagnostics-eventflow/issues/255 for how this may happen
            throw new IOException("File system is read only");
        }
    }
}
