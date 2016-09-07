// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Microsoft.Diagnostics.EventFlow.HealthReporters
{
    internal interface INewReportFileTrigger
    {
        // Event triggers when new report file should be requested.
        event EventHandler<EventArgs> NewReportFileRequested;
    }
}
