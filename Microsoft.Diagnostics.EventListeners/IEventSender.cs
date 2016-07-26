// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventListeners
{
    public interface IEventSender<EventDataType>
    {
        Task SendEvents(IReadOnlyCollection<EventDataType> events, long transmissionSequenceNumber, CancellationToken cancellationToken);
    }
}