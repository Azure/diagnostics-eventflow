// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    internal class StandardTraceEventSession : ITraceEventSession
    {
        private TraceEventSession inner;
        private bool isProcessing;
        private IHealthReporter healthReporter;

        public StandardTraceEventSession(IHealthReporter healthReporter)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            this.inner = new TraceEventSession($"EventFlow-{nameof(EtwInput)}-{Guid.NewGuid().ToString()}", TraceEventSessionOptions.Create);
            this.isProcessing = false;
        }

        public void Dispose()
        {
            if (this.inner != null)
            {
                // TraceEventSession.StopOnDispose is true by default so there is no need to Stop() the session explicitly.
                this.inner.Dispose();
                this.inner = null;
            }
        }

        public void EnableProvider(Guid providerGuid, TraceEventLevel maximumEventLevel, ulong enabledKeywords)
        {
            if (this.inner == null)
            {
                throw new ObjectDisposedException(nameof(StandardTraceEventSession));
            }

            this.inner.EnableProvider(providerGuid, maximumEventLevel, enabledKeywords);
        }

        public void EnableProvider(string providerName, TraceEventLevel maximumEventLevel, ulong enabledKeywords)
        {
            if (this.inner == null)
            {
                throw new ObjectDisposedException(nameof(StandardTraceEventSession));
            }

            this.inner.EnableProvider(providerName, maximumEventLevel, enabledKeywords);
        }

        public void Process(Action<EventData> onEvent)
        {
            if (this.inner == null)
            {
                throw new ObjectDisposedException(nameof(StandardTraceEventSession));
            }

            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }

            if (!isProcessing)
            {
                isProcessing = true;
                this.inner.Source.Dynamic.All += (traceEvent) => onEvent(traceEvent.ToEventData(this.healthReporter));
                this.inner.Source.Process();
            }
        }
    }
}
