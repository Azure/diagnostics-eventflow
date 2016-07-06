// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventListeners
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Table;

    public class TableStorageEventListener : BufferingEventListener
    {
        private const int MaxConcurrentPartitions = 4;
        private const string KeySegmentSeparator = "_";
        private readonly string instanceId;
        private CloudTable cloudTable;
        private volatile int nextEntityId;
        private object identityIdResetLock;

        public TableStorageEventListener(IConfigurationProvider configurationProvider, IHealthReporter healthReporter)
            : base(configurationProvider, healthReporter)
        {
            if (this.Disabled)
            {
                return;
            }

            Debug.Assert(configurationProvider != null);
            this.CreateTableClient(configurationProvider);

            Random randomNumberGenerator = new Random();
            this.instanceId = randomNumberGenerator.Next(100000000).ToString("D8");

            this.nextEntityId = 0;
            this.identityIdResetLock = new object();

            this.Sender = new ConcurrentEventSender<EventData>(
                eventBufferSize: 1000,
                maxConcurrency: MaxConcurrentPartitions,
                batchSize: 50,
                noEventsDelay: TimeSpan.FromMilliseconds(1000),
                transmitterProc: this.SendEventsAsync,
                healthReporter: healthReporter);
        }

        private void CreateTableClient(IConfigurationProvider configurationProvider)
        {
            string accountConnectionString = configurationProvider.GetValue("StorageAccountConnectionString");
            string sasToken = configurationProvider.GetValue("StorageAccountSasToken");

            if (string.IsNullOrWhiteSpace(sasToken) && string.IsNullOrWhiteSpace(accountConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "Configuration must specify either the storage account connection string ('StorageAccountConnectionString' parameter) or SAS token ('StorageAccountSasToken' paramteter)");
            }

            string storageTableName = configurationProvider.GetValue("StorageTableName");
            if (string.IsNullOrWhiteSpace(storageTableName))
            {
                throw new ConfigurationErrorsException("Configuration must specify the target storage name ('storageTableName' parameter)");
            }

            CloudStorageAccount storageAccount = string.IsNullOrWhiteSpace(sasToken)
                ? CloudStorageAccount.Parse(accountConnectionString)
                : new CloudStorageAccount(new StorageCredentials(sasToken), useHttps: true);
            this.cloudTable = storageAccount.CreateCloudTableClient().GetTableReference(storageTableName);

            try
            {
                this.cloudTable.CreateIfNotExists();
            }
            catch (Exception e)
            {
                this.ReportListenerProblem("Could not ensure that destination Azure storage table exists" + Environment.NewLine + e.ToString());
                throw;
            }
        }

        private async Task SendEventsAsync(IEnumerable<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (events == null)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            string partitionKey = now.ToString("yyyyMMddhhmm") + KeySegmentSeparator + (transmissionSequenceNumber % MaxConcurrentPartitions).ToString("D2");
            string rowKeyPrefix = now.ToString("ssfff");

            try
            {
                TableBatchOperation batchOperation = new TableBatchOperation();

                foreach (EventData eventData in events)
                {
                    DynamicTableEntity entity = this.ToTableEntity(eventData, partitionKey, rowKeyPrefix);
                    TableOperation insertOperation = TableOperation.Insert(entity);
                    batchOperation.Add(insertOperation);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // CONSIDER exposing TableRequestOptions and OperationContext for the batch operation
                await this.cloudTable.ExecuteBatchAsync(batchOperation, null, null, cancellationToken);

                this.ReportListenerHealthy();
            }
            catch (Exception e)
            {
                this.ReportListenerProblem("Diagnostics data upload has failed." + Environment.NewLine + e.ToString());
            }
        }

        private DynamicTableEntity ToTableEntity(EventData eventData, string partitionKey, string rowKeyPrefix)
        {
            DynamicTableEntity result = new DynamicTableEntity();
            result.PartitionKey = partitionKey;
            result.RowKey = rowKeyPrefix + KeySegmentSeparator + this.GetEntitySequenceId().ToString() + KeySegmentSeparator + this.instanceId;

            result.Properties.Add(nameof(eventData.Timestamp), new EntityProperty(eventData.Timestamp));
            result.Properties.Add(nameof(eventData.ProviderName), new EntityProperty(eventData.ProviderName));
            result.Properties.Add(nameof(eventData.EventId), new EntityProperty(eventData.EventId));
            result.Properties.Add(nameof(eventData.Message), new EntityProperty(eventData.Message));
            result.Properties.Add(nameof(eventData.Level), new EntityProperty(eventData.Level));
            result.Properties.Add(nameof(eventData.Keywords), new EntityProperty(eventData.Keywords));
            result.Properties.Add(nameof(eventData.EventName), new EntityProperty(eventData.EventName));

            foreach (KeyValuePair<string, object> item in eventData.Payload)
            {
                result.Properties.Add(item.Key, EntityProperty.CreateEntityPropertyFromObject(item.Value));
            }

            return result;
        }

        private int GetEntitySequenceId()
        {
            int result = Interlocked.Increment(ref this.nextEntityId);

            // We do not want the nextIdentityId to ever become negative, but we also want new IDs to be acquired fast, without locking.
            // So we will reset the value before it reaches int.MaxValue. As long as concurrent writers cannot acquire more
            // than (padding) values before nextEntityId is reset, we are fine.
            const int MaxEntityId = int.MaxValue - MaxConcurrentPartitions;
            if (result >= MaxEntityId)
            {
                lock (this.identityIdResetLock)
                {
                    if (this.nextEntityId >= MaxEntityId)
                    {
                        this.nextEntityId = 0;
                    }
                }
            }

            return result;
        }
    }
}