// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class TraceInput : TraceListener, IObservable<EventData>, IDisposable
    {
        public static readonly string TraceTag = nameof(TraceInput);

        private EventFlowSubject<EventData> subject;
        private readonly IHealthReporter healthReporter;

        public TraceInput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Validation.Requires.NotNull(configuration, nameof(configuration));
            Validation.Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;

            TraceInputConfiguration traceInputConfiguration = new TraceInputConfiguration();
            bool configurationIsValid = true;
            try
            {
                configuration.Bind(traceInputConfiguration);
            }
            catch
            {
                configurationIsValid = false;
            }
            if (!configurationIsValid || !string.Equals(traceInputConfiguration.Type, "trace", StringComparison.OrdinalIgnoreCase))
            {
                healthReporter.ReportWarning($"Invalid {nameof(TraceInput)} configuration encountered: '{configuration.ToString()}'. Error will be used as trace level",
                    EventFlowContextIdentifiers.Configuration);
            }

            Initialize(traceInputConfiguration);
        }

        public TraceInput(TraceInputConfiguration traceInputConfiguration, IHealthReporter healthReporter)
        {
            Validation.Requires.NotNull(traceInputConfiguration, nameof(traceInputConfiguration));
            Validation.Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;

            Initialize(traceInputConfiguration);
        }

        #region Overrides for TraceListener
        public override void Write(string message)
        {
            WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            SubmitEventData(message, TraceEventType.Information);
        }

        public override void Fail(string message)
        {
            Fail(message, null);
        }

        public override void Fail(string message, string detailMessage)
        {
            if (!string.IsNullOrWhiteSpace(detailMessage))
            {
                message = message + Environment.NewLine + detailMessage;
            }
            SubmitEventData(message, TraceEventType.Error);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            if (this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
            {
                return;
            }
            string message = string.Empty;
            if (data != null)
            {
                message = data.ToString();
            }
            SubmitEventData(message, eventType, id, source);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            if (this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
            {
                return;
            }
            StringBuilder stringBuilder = new StringBuilder();
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (i != 0)
                    {
                        stringBuilder.Append(", ");
                    }
                    if (data[i] != null)
                    {
                        stringBuilder.Append(data[i].ToString());
                    }
                }
            }
            SubmitEventData(stringBuilder.ToString(), eventType, id, source);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                return;
            }
            SubmitEventData(message, eventType, id, source);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
            {
                return;
            }
            string message = null;
            if (args != null)
            {
                message = string.Format(CultureInfo.InvariantCulture, format, args);
            }
            else
            {
                message = format;
            }
            SubmitEventData(message, eventType, id, source);
        }

#if !NETSTANDARD1_6
        public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
        {
            SubmitEventData(message, TraceEventType.Transfer, id, source, relatedActivityId.ToString());
        }
#endif
        #endregion

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return this.subject.Subscribe(observer);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.subject.Dispose();
        }

        private void Initialize(TraceInputConfiguration traceInputConfiguration)
        {
            Debug.Assert(traceInputConfiguration != null);
            Debug.Assert(this.healthReporter != null);

            this.subject = new EventFlowSubject<EventData>();

            Filter = new EventTypeFilter(traceInputConfiguration.TraceLevel);
            Trace.Listeners.Add(this);
            this.healthReporter.ReportHealthy($"{nameof(TraceInput)} initialized.", TraceTag);
        }

        private void SubmitEventData(string message, TraceEventType level, int? id = null, string source = null, string relatedActivityId = null)
        {
            try
            {
                EventData eventEntry = new EventData()
                {
                    ProviderName = string.IsNullOrEmpty(source) ? TraceTag : source,
                    Timestamp = DateTime.UtcNow,
                    Level = ToEventLevel(level)
                };

                var eventPayload = eventEntry.Payload;
                eventPayload["Message"] = message;

                if (id != null && id.HasValue)
                {
                    eventPayload["EventId"] = id.Value;
                }

                if (!string.IsNullOrEmpty(relatedActivityId))
                {
                    eventPayload["RelatedActivityID"] = relatedActivityId;
                }

                this.subject.OnNext(eventEntry);
            }
            catch (Exception ex)
            {
                this.healthReporter?.ReportProblem($"Failed to write message. Error: {ex.ToString()}", TraceTag);
            }
        }

        private LogLevel ToEventLevel(TraceEventType traceEventType)
        {
            if ((traceEventType & TraceEventType.Critical) != 0)
            {
                return LogLevel.Critical;
            }

            if ((traceEventType & TraceEventType.Error) != 0)
            {
                return LogLevel.Error;
            }

            if ((traceEventType & TraceEventType.Warning) != 0)
            {
                return LogLevel.Warning;
            }

            if ((traceEventType & TraceEventType.Information) != 0)
            {
                return LogLevel.Informational;
            }

            if ((traceEventType & TraceEventType.Verbose) != 0)
            {
                return LogLevel.Verbose;
            }

            // Assume informational if the trace event type has no explicit level set
            return LogLevel.Informational;
        }
    }
}
