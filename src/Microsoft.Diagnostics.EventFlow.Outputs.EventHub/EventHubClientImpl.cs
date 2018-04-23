// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    internal class EventHubClientImpl : IEventHubClient
    {
        private EventHubClient inner;

        public EventHubClientImpl(EventHubClient inner)
        {
            Requires.NotNull(inner, nameof(inner));
            this.inner = inner;
        }

        public Task SendAsync(IEnumerable<Azure.EventHubs.EventData> batch)
        {
            return this.inner.SendAsync(batch);
        }
    }
}
