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

                var eventPayload = eventEntry.Payload;
                InvokeAndReport(() => eventPayload.AddOrDuplicate("Message", message));
                InvokeAndReport(() => eventPayload.AddOrDuplicate("EventId", eventId.Id));

                if (eventId.Name != null)
                {
                    InvokeAndReport(() => eventPayload.AddOrDuplicate("EventName", eventId.Name));
                }
                if (exception != null)
                {
                    InvokeAndReport(() => eventPayload.AddOrDuplicate("Exception", exception));
                }
                if (payload != null)
                {
                    foreach (var kv in payload)
                        InvokeAndReport(() => eventPayload.AddOrDuplicate(kv));
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

        private void InvokeAndReport(Func<DictionaryExtenstions.AddResult> action)
        {
            Debug.Assert(action != null);
            var result = action.Invoke();
            if (result.KeyChanged)
            {
                this.healthReporter.ReportWarning(
                    $"The property with the key \"{result.OldKey}\" already exist in the event payload. Value was added under key \"{result.NewKey}\"",
                    nameof(EventFlowLogger));
            }
        }
    }
}
