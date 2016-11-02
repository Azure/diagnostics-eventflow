// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Events;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class SerilogInput : IObserver<LogEvent>, IObservable<EventData>, IDisposable
    {
        private static readonly IDictionary<LogEventLevel, LogLevel> ToLogLevel =
            new Dictionary<LogEventLevel, LogLevel>
            {
                [LogEventLevel.Verbose] = LogLevel.Verbose,
                [LogEventLevel.Debug] = LogLevel.Verbose,
                [LogEventLevel.Information] = LogLevel.Informational,
                [LogEventLevel.Warning] = LogLevel.Warning,
                [LogEventLevel.Error] = LogLevel.Error,
                [LogEventLevel.Fatal] = LogLevel.Critical
            };

        private EventFlowSubject<EventData> subject;
        private readonly IHealthReporter healthReporter;

        public SerilogInput(IHealthReporter healthReporter)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            this.subject = new EventFlowSubject<EventData>();
        }

        void IObserver<LogEvent>.OnCompleted()
        {
            this.subject.OnCompleted();
        }

        void IObserver<LogEvent>.OnError(Exception error)
        {
            this.subject.OnError(error);
        }

        void IObserver<LogEvent>.OnNext(LogEvent value)
        {
            if (value == null)
            {
                return;
            }

            EventData e = ToEventData(value);
            this.subject.OnNext(e);
        }
        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return this.subject.Subscribe(observer);
        }

        public virtual void Dispose()
        {
            this.subject.Dispose();
        }

        private EventData ToEventData(LogEvent logEvent)
        {
            EventData eventData = new EventData
            {
                ProviderName = nameof(SerilogInput),
                Timestamp = logEvent.Timestamp,
                Level = ToLogLevel[logEvent.Level],
                Keywords = 0
            };

            var payload = eventData.Payload;

            if (logEvent.Exception != null)
            {
                payload["Exception"] = logEvent.Exception;
            }

            // Inability to render the message, or any other LogEvent property, should not stop us from sending the event down the pipeline
            try
            {
                payload["Message"] = logEvent.RenderMessage();
            }
            catch (Exception e)
            {
                healthReporter.ReportWarning($"{nameof(SerilogInput)}: event message could not be rendered{Environment.NewLine}{e.ToString()}");
            }

            foreach (var property in logEvent.Properties.Where(property => property.Value != null))
            {
                try
                {
                    payload[property.Key] = property.Value.ToString();
                }
                catch (Exception e)
                {
                    healthReporter.ReportWarning($"{nameof(SerilogInput)}: event property '{property.Key}' could not be rendered{Environment.NewLine}{e.ToString()}");
                }
            }

            return eventData;
        }
    }
}
