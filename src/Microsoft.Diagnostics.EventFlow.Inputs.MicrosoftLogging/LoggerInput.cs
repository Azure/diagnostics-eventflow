// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class LoggerInput : IObservable<EventData>, IDisposable
    {
        public static readonly string TraceTag = nameof(LoggerInput);

        private readonly EventFlowSubject<EventData> subject;
        private readonly IHealthReporter healthReporter;

        public LoggerInput(IHealthReporter healthReporter)
        {
            Validation.Requires.NotNull(healthReporter, nameof(healthReporter));
            this.healthReporter = healthReporter;
            this.subject = new EventFlowSubject<EventData>();
            this.healthReporter.ReportHealthy($"{nameof(LoggerInput)} initialized.", TraceTag);
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return this.subject.Subscribe(observer);
        }

        internal void SubmitEventData(string message, LogLevel level, EventId eventId, Exception exception, string source, Dictionary<string, object> payload)
        {
            try
            {
                EventData eventEntry = new EventData()
                {
                    ProviderName = string.IsNullOrEmpty(source) ? TraceTag : source,
                    Timestamp = DateTime.UtcNow,
                    Level = level
                };

                IDictionary<string, object> payloadData = eventEntry.Payload;
                payloadData.Add("Message", message);
                payloadData.Add("EventId", eventId.Id);

                if (eventId.Name != null)
                {
                    payloadData.Add("EventName", eventId.Name);
                }
                if (exception != null)
                {
                    payloadData.Add("Exception", exception);
                }
                if (payload != null)
                {
                    foreach (var kv in payload)
                    {
                        AddPayloadProperty(eventEntry, kv.Key, kv.Value);
                    }
                }

                this.subject.OnNext(eventEntry);
            }
            catch (Exception ex)
            {
                this.healthReporter.ReportWarning($"Failed to write message. Error: {ex.ToString()}", TraceTag);
            }
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }

        private void AddPayloadProperty(EventData eventData, string key, object value)
        {
            Debug.Assert(eventData != null);
            Debug.Assert(!string.IsNullOrEmpty(key));

            eventData.AddPayloadProperty(key, value, this.healthReporter, nameof(EventFlowLogger));
        }
    }
}
