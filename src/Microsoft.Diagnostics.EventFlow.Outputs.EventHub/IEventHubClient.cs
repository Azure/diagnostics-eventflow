// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using MessagingEventData = Azure.Messaging.EventHubs.EventData;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    public interface IEventHubClient
    {
        Task SendAsync(IEnumerable<MessagingEventData> batch);
        Task SendAsync(IEnumerable<MessagingEventData> batch, string partitionKey);
        Task CloseAsync();
    }
}
