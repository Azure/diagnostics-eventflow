// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics
{
    public abstract class OutputBase: ThrottledHealthInformationSource, IOutput
    {
        public OutputBase(IHealthReporter healthReporter) : base(healthReporter) { }

        protected delegate void ProcessPayload<T>(T value);

        public abstract Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken);

        protected void GetValueFromPayload<T>(EventData e, string payloadName, ProcessPayload<T> handler)
        {
            if (string.IsNullOrEmpty(payloadName))
            {
                return;
            }

            object p = e.Payload[payloadName];
            if (p != null)
            {
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
            }
        }
    }
}
