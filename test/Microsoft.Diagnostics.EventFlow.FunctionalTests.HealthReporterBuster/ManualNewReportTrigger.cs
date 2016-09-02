// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Diagnostics.EventFlow.Core.Implementations.HealthReporters;

namespace Microsoft.Diagnostics.EventFlow.FunctionalTests.HealthReporterBuster
{
    internal class ManualNewReportTrigger : INewReportTrigger
    {
        public event EventHandler<EventArgs> Triggered;

        public void TriggerChange()
        {
            Triggered?.Invoke(this, EventArgs.Empty);
        }
    }
}
