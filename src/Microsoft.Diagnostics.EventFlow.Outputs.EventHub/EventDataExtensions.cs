// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Text;
using Newtonsoft.Json;
using MessagingEventData = Microsoft.ServiceBus.Messaging.EventData;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    internal static class EventDataExtensions
    {
        public static MessagingEventData ToMessagingEventData(this EventData eventData)
        {
            string eventDataSerialized = JsonConvert.SerializeObject(eventData);
            MessagingEventData messagingEventData = new MessagingEventData(Encoding.UTF8.GetBytes(eventDataSerialized));
            return messagingEventData;
        }
    }
}
