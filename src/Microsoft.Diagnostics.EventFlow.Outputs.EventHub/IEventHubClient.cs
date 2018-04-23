// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using MessagingEventData = Microsoft.Azure.EventHubs.EventData;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    public interface IEventHubClient
    {
        Task SendAsync(IEnumerable<MessagingEventData> batch);
    }
}
