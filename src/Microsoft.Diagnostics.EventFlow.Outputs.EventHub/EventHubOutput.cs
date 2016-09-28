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

    public class EventHubOutput : OutputBase, IDisposable
    {
        // EventHub has a limit of 262144 bytes per message. We are only allowing ourselves of that minus 16k, in case they have extra stuff
        // that tag onto the message. If it exceeds that, we need to break it up into multiple batches.
        private const int EventHubMessageSizeLimit = 262144 - 16384;

        private const int ConcurrentConnections = 4;
        private string eventHubName;

        // This connections field will be used by multi-threads. Throughout this class, try to use Interlocked methods to load the field first,
        // before accessing to guarantee your function won't be affected by another thread.
        private EventHubConnection[] connections;

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
            // Get a reference to the current connections array first, just in case there is another thread wanting to clean
            // up the connections with CleanUpAsync(), we won't get a null reference exception here.
            EventHubConnection[] currentConnections = Interlocked.CompareExchange<EventHubConnection[]>(ref this.connections, this.connections, this.connections);

            if (currentConnections == null || events == null || events.Count == 0)
            {
                return;
            }

            try
            {
                // Since event hub limits each message/batch to be a certain size, we need to
                // keep checking the size for exceeds and split into a new batch as needed

                List<List<MessagingEventData>> batches = new List<List<MessagingEventData>>();
                int batchByteSize = 0;

                foreach (EventData eventData in events)
                {
                    int messageSize;
                    MessagingEventData messagingEventData = eventData.ToMessagingEventData(out messageSize);

                    // If we don't have a batch yet, or the addition of this message will exceed the limit for this batch, then
                    // start a new batch.
                    if (batches.Count == 0 ||
                        batchByteSize + messageSize > EventHubMessageSizeLimit)
                    {
                        batches.Add(new List<MessagingEventData>());
                        batchByteSize = 0;
                    }

                    batchByteSize += messageSize;

                    List<MessagingEventData> currentBatch = batches[batches.Count - 1];
                    currentBatch.Add(messagingEventData);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                EventHubClient hubClient = currentConnections[transmissionSequenceNumber % ConcurrentConnections].HubClient;

                List<Task> tasks = new List<Task>();
                foreach (List<MessagingEventData> batch in batches)
                {
                    tasks.Add(hubClient.SendBatchAsync(batch));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                this.healthReporter.ReportHealthy();
            }
            catch (Exception e)
            {
                string errorMessage = nameof(EventHubOutput) + ": diagnostics data upload has failed." + Environment.NewLine + e.ToString();
                this.healthReporter.ReportProblem(errorMessage);
            }
        }

        // The Initialize method is not thread-safe. Please only call this on one thread and do so before the pipeline starts sending
        // data to this output
        private void Initialize(EventHubOutputConfiguration configuration)
        {
            Debug.Assert(configuration != null);
            Debug.Assert(this.healthReporter != null);

            if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
            {
                var errorMessage = $"{nameof(EventHubOutput)}: '{nameof(EventHubOutputConfiguration.ConnectionString)}' configuration parameter must be set to a valid connection string";
                healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }

            ServiceBusConnectionStringBuilder connStringBuilder = new ServiceBusConnectionStringBuilder(configuration.ConnectionString);
            connStringBuilder.TransportType = TransportType.Amqp;

            this.eventHubName = connStringBuilder.EntityPath ?? configuration.EventHubName;
            if (string.IsNullOrWhiteSpace(this.eventHubName))
            {
                var errorMessage = $"{nameof(EventHubOutput)}: Event Hub name must not be empty. It can be specified in the '{nameof(EventHubOutputConfiguration.ConnectionString)}' or '{nameof(EventHubOutputConfiguration.EventHubName)}' configuration parameter";

                healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }

            this.connections = new EventHubConnection[ConcurrentConnections];

            // To create a MessageFactory, the connection string can't contain the EntityPath. So we set it to null here.
            connStringBuilder.EntityPath = null;
            for (uint i = 0; i < this.connections.Length; i++)
            {
                MessagingFactory factory = MessagingFactory.CreateFromConnectionString(connStringBuilder.ToString());
                this.connections[i] = new EventHubConnection();
                this.connections[i].MessagingFactory = factory;
                this.connections[i].HubClient = factory.CreateEventHubClient(this.eventHubName);
            }
        }

        void IDisposable.Dispose()
        {
            // Just fire and forget
            CleanUpAsync();
        }

        private async void CleanUpAsync()
        {
            // Swap out the connections first, so all other callers will see this as uninitialized
            EventHubConnection[] oldConnections = Interlocked.Exchange<EventHubConnection[]>(ref this.connections, null);

            // HubClients must be closed before the messaging factories, so hence we close in this order
            List<Task> tasks = new List<Task>();
            foreach (EventHubConnection connection in oldConnections)
            {
                tasks.Add(connection.HubClient.CloseAsync());
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);

            tasks = new List<Task>();
            foreach (EventHubConnection connection in oldConnections)
            {
                tasks.Add(connection.MessagingFactory.CloseAsync());
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private class EventHubConnection
        {
            public MessagingFactory MessagingFactory;
            public EventHubClient HubClient;
        }
    }
}