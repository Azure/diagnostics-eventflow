// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using MessagingEventData = Microsoft.ServiceBus.Messaging.EventData;

namespace Microsoft.Diagnostics.EventListeners
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class EventHubListener : BufferingEventListener
    {
        private const int ConcurrentConnections = 4;
        private EventHubConnectionData connectionData;

        public EventHubListener(IConfigurationProvider configurationProvider, IHealthReporter healthReporter) : base(configurationProvider, healthReporter)
        {
            if (this.Disabled)
            {
                return;
            }

            Debug.Assert(configurationProvider != null);
            this.CreateConnectionData(configurationProvider);

            this.Sender = new ConcurrentEventSender<EventData>(
                eventBufferSize: 1000,
                maxConcurrency: ConcurrentConnections,
                batchSize: 50,
                noEventsDelay: TimeSpan.FromMilliseconds(1000),
                transmitterProc: this.SendEventsAsync,
                healthReporter: healthReporter);
        }

        private void CreateConnectionData(object sender)
        {
            IConfigurationProvider configurationProvider = (IConfigurationProvider) sender;

            string serviceBusConnectionString = configurationProvider.GetValue("serviceBusConnectionString");
            if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "Configuraiton parameter 'serviceBusConnectionString' must be set to a valid Service Bus connection string");
            }

            string eventHubName = configurationProvider.GetValue("eventHubName");
            if (string.IsNullOrWhiteSpace(eventHubName))
            {
                throw new ConfigurationErrorsException("Configuration parameter 'eventHubName' must not be empty");
            }

            this.connectionData = new EventHubConnectionData();
            this.connectionData.EventHubName = eventHubName;

            ServiceBusConnectionStringBuilder connStringBuilder = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
            connStringBuilder.TransportType = TransportType.Amqp;
            this.connectionData.MessagingFactories = new MessagingFactory[ConcurrentConnections];
            for (uint i = 0; i < ConcurrentConnections; i++)
            {
                this.connectionData.MessagingFactories[i] = MessagingFactory.CreateFromConnectionString(connStringBuilder.ToString());
            }
        }

        private async Task SendEventsAsync(IEnumerable<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (events == null)
            {
                return;
            }

            try
            {
                List<MessagingEventData> batch = new List<MessagingEventData>();

                foreach (EventData eventData in events)
                {
                    MessagingEventData messagingEventData = eventData.ToMessagingEventData();
                    batch.Add(messagingEventData);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                MessagingFactory factory = this.connectionData.MessagingFactories[transmissionSequenceNumber%ConcurrentConnections];
                EventHubClient hubClient;
                lock (factory)
                {
                    hubClient = factory.CreateEventHubClient(this.connectionData.EventHubName);
                }

                await hubClient.SendBatchAsync(batch);

                this.ReportListenerHealthy();
            }
            catch (Exception e)
            {
                this.ReportListenerProblem("Diagnostics data upload has failed." + Environment.NewLine + e.ToString());
            }
        }

        private class EventHubConnectionData
        {
            public string EventHubName;
            public MessagingFactory[] MessagingFactories;
        }
    }
}