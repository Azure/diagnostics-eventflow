// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace Microsoft.Diagnostics.EventListeners
{
    public abstract class SenderBase<EventDataType>: IEventSender<EventDataType>
    {
        protected readonly IHealthReporter healthReporter;
        private TimeSpanThrottle errorReportingThrottle;

        public SenderBase(IHealthReporter healthReporter)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));
            this.healthReporter = healthReporter;
            this.errorReportingThrottle = new TimeSpanThrottle(TimeSpan.FromSeconds(1));
        }

        public abstract Task SendEventsAsync(IReadOnlyCollection<EventDataType> events, long transmissionSequenceNumber, CancellationToken cancellationToken);

        protected void ReportSenderHealthy()
        {
            this.errorReportingThrottle.Execute(() => this.healthReporter.ReportHealthy());
        }

        protected void ReportSenderProblem(string problemDescription)
        {
            this.errorReportingThrottle.Execute(() => this.healthReporter.ReportProblem(problemDescription));
        }
    }
}
