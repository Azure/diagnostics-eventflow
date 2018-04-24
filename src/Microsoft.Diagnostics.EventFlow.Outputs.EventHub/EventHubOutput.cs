﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Utilities;
using Microsoft.Extensions.Configuration;
using Validation;
using MessagingEventData = Microsoft.Azure.EventHubs.EventData;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{

    public class EventHubOutput : IOutput, IDisposable
    {
        // EventHub has a limit of 262144 bytes per message. We are only allowing ourselves of that minus 16k, in case they have extra stuff
        // that tag onto the message. If it exceeds that, we need to break it up into multiple batches.
        private const int EventHubMessageSizeLimit = 262144 - 16384;

        private const int ConcurrentConnections = 4;
        private string eventHubName;

        // Clients field will be used by multi-threads. Throughout this class, try to use Interlocked methods to load the field first,
        // before accessing to guarantee your function won't be affected by another thread.
        private IEventHubClient[] clients;
        private Func<string, IEventHubClient> eventHubClientFactory;
        private EventHubOutputConfiguration outputConfiguration;
        private readonly IHealthReporter healthReporter;

        public EventHubOutput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            this.eventHubClientFactory = this.CreateEventHubClient;
            this.outputConfiguration = new EventHubOutputConfiguration();
            try
            {
                configuration.Bind(this.outputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(EventHubOutput)} configuration encountered: '{configuration.ToString()}'",
                    EventFlowContextIdentifiers.Configuration);
                throw;
            }

            Initialize();
        }

        public EventHubOutput(
            EventHubOutputConfiguration eventHubOutputConfiguration, 
            IHealthReporter healthReporter, 
            Func<string, IEventHubClient> eventHubClientFactory = null)
        {
            Requires.NotNull(eventHubOutputConfiguration, nameof(eventHubOutputConfiguration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            this.eventHubClientFactory = eventHubClientFactory ?? this.CreateEventHubClient;
            this.outputConfiguration = eventHubOutputConfiguration;
            Initialize();
        }

        public async Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            // Get a reference to the current connections array first, just in case there is another thread wanting to clean
            // up the connections with CleanUpAsync(), we won't get a null reference exception here.
            IEventHubClient[] currentClients = Interlocked.CompareExchange<IEventHubClient[]>(ref this.clients, this.clients, this.clients);

            if (currentClients == null || events == null || events.Count == 0)
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

                IEventHubClient hubClient = currentClients[transmissionSequenceNumber % ConcurrentConnections];

                List<Task> tasks = new List<Task>();
                foreach (List<MessagingEventData> batch in batches)
                {
                    tasks.Add(hubClient.SendAsync(batch));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                this.healthReporter.ReportHealthy();
            }
            catch (Exception e)
            {
                ErrorHandlingPolicies.HandleOutputTaskError(e, () => 
                {
                    string errorMessage = nameof(EventHubOutput) + ": diagnostics data upload has failed." + Environment.NewLine + e.ToString();
                    this.healthReporter.ReportWarning(errorMessage, EventFlowContextIdentifiers.Output);
                });
            }
        }

        // The Initialize method is not thread-safe. Please only call this on one thread and do so before the pipeline starts sending
        // data to this output
        private void Initialize()
        {
            Debug.Assert(this.healthReporter != null);

            if (string.IsNullOrWhiteSpace(this.outputConfiguration.ConnectionString))
            {
                var errorMessage = $"{nameof(EventHubOutput)}: '{nameof(EventHubOutputConfiguration.ConnectionString)}' configuration parameter must be set to a valid connection string";
                healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }

            this.clients = new IEventHubClient[ConcurrentConnections];
            for (uint i = 0; i < this.clients.Length; i++)
            {
                this.clients[i] = this.eventHubClientFactory(this.outputConfiguration.ConnectionString);
            }
        }

        void IDisposable.Dispose()
        {
            // Just fire and forget
            CleanUpAsync();
        }

        private async void CleanUpAsync()
        {
            // Swap out the clients first, so all other callers will see this as uninitialized
            IEventHubClient[] oldClients = Interlocked.Exchange<IEventHubClient[]>(ref this.clients, null);

            List<Task> tasks = new List<Task>();
            foreach (IEventHubClient client in oldClients)
            {
                tasks.Add(client.CloseAsync());
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private IEventHubClient CreateEventHubClient(string connectionString)
        {
            Debug.Assert(this.outputConfiguration.ConnectionString != null);
            EventHubsConnectionStringBuilder connStringBuilder = new EventHubsConnectionStringBuilder(this.outputConfiguration.ConnectionString);

            this.eventHubName = connStringBuilder.EntityPath ?? this.outputConfiguration.EventHubName;
            if (string.IsNullOrWhiteSpace(this.eventHubName))
            {
                var errorMessage = $"{nameof(EventHubOutput)}: Event Hub name must not be empty. It can be specified in the '{nameof(EventHubOutputConfiguration.ConnectionString)}' or '{nameof(EventHubOutputConfiguration.EventHubName)}' configuration parameter";

                healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }
            connStringBuilder.EntityPath = this.eventHubName;

            return new EventHubClientImpl(EventHubClient.CreateFromConnectionString(connStringBuilder.ToString()));
        }
    }
}