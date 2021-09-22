// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Producer;
using Validation;
using MessagingEventData = Azure.Messaging.EventHubs.EventData;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    internal class EventHubClientImpl : IEventHubClient
    {
        private EventHubProducerClient inner;

        public EventHubClientImpl(EventHubProducerClient inner)
        {
            Requires.NotNull(inner, nameof(inner));
            this.inner = inner;
        }

        public Task SendAsync(IEnumerable<MessagingEventData> batch)
        {
            return this.inner.SendAsync(batch);
        }

        public Task SendAsync(IEnumerable<MessagingEventData> batch, string partitionKey)
        {
            var sendOptions = new SendEventOptions
            {
                PartitionKey = partitionKey
            };

            return this.inner.SendAsync(batch, sendOptions);
        }

        public Task CloseAsync()
        {
            return this.inner.CloseAsync();
        }
    }
}
