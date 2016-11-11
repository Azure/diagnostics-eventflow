// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
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
                eventPayload["Message"] = message;
                eventPayload["EventId"] = eventId.Id;
                if (eventId.Name != null)
                {
                    eventPayload["EventName"] = eventId.Name;
                }
                if (exception != null)
                {
                    eventPayload["Exception"] = exception;
                }
                if (payload != null)
                {
                    foreach (var kv in payload)
                        eventPayload.Add(kv);
                }

                this.subject.OnNext(eventEntry);
            }
            catch (Exception ex)
            {
                this.healthReporter?.ReportProblem($"Failed to write message. Error: {ex}", TraceTag);
            }
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }
    }
}
