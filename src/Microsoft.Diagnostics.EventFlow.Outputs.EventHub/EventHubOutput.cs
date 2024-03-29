﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Utilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Validation;
using MessagingEventData = Azure.Messaging.EventHubs.EventData;

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
        private Func<EventHubOutputConfiguration, IEventHubClient> eventHubClientFactory;
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
            Func<EventHubOutputConfiguration, IEventHubClient> eventHubClientFactory = null)
        {
            Requires.NotNull(eventHubOutputConfiguration, nameof(eventHubOutputConfiguration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            this.eventHubClientFactory = eventHubClientFactory ?? this.CreateEventHubClient;
            this.outputConfiguration = eventHubOutputConfiguration;
            Initialize();
        }

        public JsonSerializerSettings SerializerSettings { get; set; }

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
                var groupedEventData = events.GroupBy(e => string.IsNullOrEmpty(outputConfiguration.PartitionKeyProperty) == false && e.TryGetPropertyValue(outputConfiguration.PartitionKeyProperty, out var partionKeyData) ? partionKeyData.ToString() : string.Empty, e => e);

                List<Task> tasks = new List<Task>();

                foreach (var partitionedEventData in groupedEventData)
                {
                    //assemble the full list of MessagingEventData items plus their messageSize
                    List<(MessagingEventData message, int messageSize)> batchRecords = partitionedEventData.Select(
                        e => new Tuple<MessagingEventData, int>(e.ToMessagingEventData(SerializerSettings, out var messageSize), messageSize).ToValueTuple()).ToList();

                    SendBatch(batchRecords);

                    void SendBatch(IReadOnlyCollection<(MessagingEventData message, int messageSize)> batch)
                    {
                        // Since event hub limits each message/batch to be a certain size, we need to
                        // keep checking the size in bytes of the batch and recursively keep splitting into two batches as needed
                        if (batch.Count >= 2 && batch.Sum(b => b.messageSize) > EventHubMessageSizeLimit)
                        {
                            //the batch total message size is too big to send to EventHub, but it still contains at least two items,
                            //so we split the batch up in half and recusively call the inline SendBatch() method with the two new smaller batches
                            var indexMiddle = batch.Count / 2;
                            SendBatch(batch.Take(indexMiddle).ToList());
                            SendBatch(batch.Skip(indexMiddle).ToList());
                            return;
                        }

                        IEventHubClient hubClient = currentClients[transmissionSequenceNumber % ConcurrentConnections];
                        if (string.IsNullOrEmpty(partitionedEventData.Key))
                        {
                            tasks.Add(hubClient.SendAsync(batch.Select(b => b.message)));
                        }
                        else
                        {
                            tasks.Add(hubClient.SendAsync(batch.Select(b => b.message), partitionedEventData.Key));
                        }
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
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

            this.clients = new IEventHubClient[ConcurrentConnections];
            for (uint i = 0; i < this.clients.Length; i++)
            {
                this.clients[i] = this.eventHubClientFactory(this.outputConfiguration);
            }

            SerializerSettings = EventFlowJsonUtilities.GetDefaultSerializerSettings();
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

        private IEventHubClient CreateEventHubClient(EventHubOutputConfiguration _)
        {
            Debug.Assert(this.outputConfiguration != null);

            if (this.outputConfiguration.UseAzureIdentity)
            {
                this.eventHubName = this.outputConfiguration.EventHubName;
                ensureEventHubName();

                if (string.IsNullOrWhiteSpace(this.outputConfiguration.FullyQualifiedNamespace))
                {
                    var emptyNamespaceMsg = $"{nameof(EventHubOutput)}: Event Hub namespace must not be empty when using Azure Identity. It can be specified in the '{nameof(EventHubOutputConfiguration.FullyQualifiedNamespace)}' configuration parameter";
                    healthReporter.ReportProblem(emptyNamespaceMsg, EventFlowContextIdentifiers.Configuration);
                    throw new Exception(emptyNamespaceMsg);
                }

                TokenCredential azureTokenCredential = this.outputConfiguration.AzureTokenCredential ?? new DefaultAzureCredential();

                return new EventHubClientImpl(
                    new EventHubProducerClient(this.outputConfiguration.FullyQualifiedNamespace, this.eventHubName, azureTokenCredential)
                );
            }

            if (!string.IsNullOrWhiteSpace(this.outputConfiguration.ConnectionString))
            {
                var connString = EventHubsConnectionStringProperties.Parse(this.outputConfiguration.ConnectionString);
                this.eventHubName = connString.EventHubName ?? this.outputConfiguration.EventHubName;
                ensureEventHubName();

                return new EventHubClientImpl(
                    new EventHubProducerClient(this.outputConfiguration.ConnectionString, this.eventHubName)
                );
            }

            var invalidConfigMsg =
                $"Invalid {nameof(EventHubOutput)} configuration encountered: '{nameof(EventHubOutputConfiguration.ConnectionString)}' value is empty and '{nameof(EventHubOutputConfiguration.UseAzureIdentity)}' is set to false. " +
                $"You need to specify either '{nameof(EventHubOutputConfiguration.ConnectionString)}' to EventHub or set '{nameof(EventHubOutputConfiguration.UseAzureIdentity)}' flag.";
            healthReporter.ReportProblem(invalidConfigMsg, EventFlowContextIdentifiers.Configuration);
            throw new Exception(invalidConfigMsg);

            void ensureEventHubName()
            {
                if (string.IsNullOrWhiteSpace(this.eventHubName))
                {
                    var emptyEventHubNameMsg = $"{nameof(EventHubOutput)}: Event Hub name must not be empty. It can be specified in the '{nameof(EventHubOutputConfiguration.ConnectionString)}' or '{nameof(EventHubOutputConfiguration.EventHubName)}' configuration parameter";
                    healthReporter.ReportProblem(emptyEventHubNameMsg);
                    throw new Exception(emptyEventHubNameMsg);
                }
            }
        }
    }
}