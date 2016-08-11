// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Validation;
using MessagingEventData = Microsoft.ServiceBus.Messaging.EventData;
using Microsoft.Extensions.Diagnostics.Senders.EventHubs;

namespace Microsoft.Extensions.Diagnostics
{

    public class EventHubSender : EventDataSender
    {
        private const int ConcurrentConnections = 4;
        private EventHubConnectionData connectionData;

        public EventHubSender(IConfiguration configuration, IHealthReporter healthReporter) : base(healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.connectionData = CreateConnectionData(configuration, healthReporter);
        }

        private EventHubConnectionData CreateConnectionData(IConfiguration configuration, IHealthReporter healthReporter)
        {
            string serviceBusConnectionString = configuration["serviceBusConnectionString"];
            if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
            {
                healthReporter.ReportProblem($"{nameof(EventHubSender)}: configuraiton parameter 'serviceBusConnectionString' must be set to a valid Service Bus connection string");
                return null;
            }

            string eventHubName = configuration["eventHubName"];
            if (string.IsNullOrWhiteSpace(eventHubName))
            {
                healthReporter.ReportProblem($"{nameof(EventHubSender)}: configuration parameter 'eventHubName' must not be empty");
                return null;
            }

            var connectionData = new EventHubConnectionData();
            connectionData.EventHubName = eventHubName;

            ServiceBusConnectionStringBuilder connStringBuilder = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
            connStringBuilder.TransportType = TransportType.Amqp;
            connectionData.MessagingFactories = new MessagingFactory[ConcurrentConnections];
            for (uint i = 0; i < ConcurrentConnections; i++)
            {
                connectionData.MessagingFactories[i] = MessagingFactory.CreateFromConnectionString(connStringBuilder.ToString());
            }

            return connectionData;
        }

        public override async Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (this.connectionData == null || events == null || events.Count == 0)
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

                this.ReportHealthy();
            }
            catch (Exception e)
            {
                this.ReportProblem($"{nameof(EventHubSender)}: diagnostics data upload has failed.{Environment.NewLine}{e.ToString()}");
            }
        }

        private class EventHubConnectionData
        {
            public string EventHubName;
            public MessagingFactory[] MessagingFactories;
        }
    }
}