// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Validation;
using MessagingEventData = Microsoft.ServiceBus.Messaging.EventData;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{

    public class EventHubOutput : OutputBase
    {
        private const int ConcurrentConnections = 4;
        private EventHubConnectionData connectionData;

        public EventHubOutput(IConfiguration configuration, IHealthReporter healthReporter) : base(healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            var eventHubOutputConfiguration = new EventHubOutputConfiguration();
            try
            {
                configuration.Bind(eventHubOutputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(EventHubOutput)} configuration encountered: '{configuration.ToString()}'",
                    EventFlowContextIdentifiers.Configuration);
                throw;
            }

            Initialize(eventHubOutputConfiguration);
        }

        public EventHubOutput(EventHubOutputConfiguration eventHubOutputConfiguration, IHealthReporter healthReporter): base(healthReporter)
        {
            Requires.NotNull(eventHubOutputConfiguration, nameof(eventHubOutputConfiguration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            Initialize(eventHubOutputConfiguration);
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

                MessagingFactory factory = this.connectionData.MessagingFactories[transmissionSequenceNumber % ConcurrentConnections];
                EventHubClient hubClient;
                lock (factory)
                {
                    hubClient = factory.CreateEventHubClient(this.connectionData.EventHubName);
                }

                await hubClient.SendBatchAsync(batch).ConfigureAwait(false);

                this.healthReporter.ReportHealthy();
            }
            catch (Exception e)
            {
                string errorMessage = nameof(EventHubOutput) + ": diagnostics data upload has failed." + Environment.NewLine + e.ToString();
                this.healthReporter.ReportProblem(errorMessage);
            }
        }

        private void Initialize(EventHubOutputConfiguration configuration)
        {
            Debug.Assert(configuration != null);
            Debug.Assert(this.healthReporter != null);

            string errorMessage;
            if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
            {
                errorMessage = $"{nameof(EventHubOutput)}: '{nameof(EventHubOutputConfiguration.ConnectionString)}' configuration parameter must be set to a valid Service Bus connection string";
                healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }

            if (string.IsNullOrWhiteSpace(configuration.EventHubName))
            {
                errorMessage = $"{nameof(EventHubOutput)}: '{nameof(EventHubOutputConfiguration.ConnectionString)}' configuration parameter must not be empty";
                healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }

            this.connectionData = new EventHubConnectionData();
            this.connectionData.EventHubName = configuration.EventHubName;

            ServiceBusConnectionStringBuilder connStringBuilder = new ServiceBusConnectionStringBuilder(configuration.ConnectionString);
            connStringBuilder.TransportType = TransportType.Amqp;
            this.connectionData.MessagingFactories = new MessagingFactory[ConcurrentConnections];
            for (uint i = 0; i < ConcurrentConnections; i++)
            {
                this.connectionData.MessagingFactories[i] = MessagingFactory.CreateFromConnectionString(connStringBuilder.ToString());
            }
        }

        private class EventHubConnectionData
        {
            public string EventHubName;
            public MessagingFactory[] MessagingFactories;
        }
    }
}