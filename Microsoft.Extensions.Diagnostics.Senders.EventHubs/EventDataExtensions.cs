using System.Text;
using Newtonsoft.Json;
using MessagingEventData = Microsoft.ServiceBus.Messaging.EventData;

namespace Microsoft.Extensions.Diagnostics.Senders.EventHubs
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
