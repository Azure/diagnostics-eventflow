// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace Microsoft.Diagnostics.EventFlow
{
    public abstract class OutputBase : IOutput
    {
        protected IHealthReporter healthReporter;

        public OutputBase(IHealthReporter healthReporter)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));
            this.healthReporter = healthReporter;
        }

        public abstract Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken);
    }
}
