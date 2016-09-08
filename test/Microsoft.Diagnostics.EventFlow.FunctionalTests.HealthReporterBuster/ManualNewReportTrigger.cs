// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Diagnostics.EventFlow.HealthReporters;

namespace Microsoft.Diagnostics.EventFlow.FunctionalTests.HealthReporterBuster
{
    internal class ManualNewReportTrigger : INewReportFileTrigger
    {
        public event EventHandler<EventArgs> NewReportFileRequested;

        public void TriggerChange()
        {
            NewReportFileRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
