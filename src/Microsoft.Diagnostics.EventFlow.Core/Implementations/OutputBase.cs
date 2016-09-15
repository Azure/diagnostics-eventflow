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

        protected delegate void ProcessPayload<T>(T value);

        public abstract Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken);

        protected bool GetValueFromPayload<T>(EventData e, string payloadName, ProcessPayload<T> handler)
        {
            if (string.IsNullOrEmpty(payloadName))
            {
                return false;
            }

            object p;
            if (!e.Payload.TryGetValue(payloadName, out p) || p == null)
            {
                return false;
            }

            bool converted = false;
            T value = default(T);

            try
            {
                value = (T)p;
                converted = true;
            }
            catch { }

            if (!converted)
            {
                try
                {
                    value = (T)Convert.ChangeType(p, typeof(T));
                    converted = true;
                }
                catch { }
            }

            if (converted)
            {
                handler(value);
            }

            return converted;
        }
    }
}
