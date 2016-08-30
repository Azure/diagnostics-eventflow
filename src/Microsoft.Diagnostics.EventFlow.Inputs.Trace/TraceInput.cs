// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class TraceInput : TraceListener, IObservable<EventData>, IDisposable
    {
        public static readonly string TraceTag = nameof(TraceInput);
        private SimpleSubject<EventData> subject;
        private readonly IHealthReporter healthReporter;
        public TraceInput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Validation.Requires.NotNull(configuration, nameof(configuration));
            Validation.Assumes.True("Trace".Equals(configuration["type"], StringComparison.OrdinalIgnoreCase), "Invalid trace configuration");
            Validation.Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            this.subject = new SimpleSubject<EventData>();

            string traceLevelString = configuration["traceLevel"];
            SourceLevels traceLevel = SourceLevels.Error;
            if (!Enum.TryParse(traceLevelString, out traceLevel))
            {
                healthReporter.ReportWarning($"Invalid trace level in configuration: {traceLevelString}. Fall back to default: {traceLevel}");
            }
            Filter = new EventTypeFilter(traceLevel);
            Trace.Listeners.Add(this);
            this.healthReporter.ReportHealthy($"{nameof(TraceInput)} initialized.", TraceTag);
        }

        #region Overrides for TraceListener
        public override void WriteLine(string message)
        {
            SubmitEventData(message, null);
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
            SubmitEventData(message, SourceLevels.Error.ToString());
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
            SubmitEventData(message, eventType.ToString(), id, source);
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
            SubmitEventData(stringBuilder.ToString(), eventType.ToString(), id, source);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                return;
            }
            SubmitEventData(message, eventType.ToString(), id, source);
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
            SubmitEventData(message, eventType.ToString(), id, source);
        }

#if !NETSTANDARD1_6
        public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
        {
            SubmitEventData(message, TraceEventType.Transfer.ToString(), id, source, relatedActivityId.ToString());
        }
#endif
        #endregion

        private void SubmitEventData(string message, string level, int? id = null, string source = null, string relatedActivityId = null)
        {
            try
            {
                EventData eventEntry = new EventData();
                eventEntry.Message = message;

                if (!string.IsNullOrEmpty(level))
                {
                    eventEntry.Level = level;
                }

                if (id != null && id.HasValue)
                {
                    eventEntry.EventId = id.Value;
                }

                if (!string.IsNullOrEmpty(source))
                {
                    eventEntry.ProviderName = source;
                }
                else
                {
                    eventEntry.ProviderName = TraceTag;
                }

                if (!string.IsNullOrWhiteSpace(relatedActivityId))
                {
                    eventEntry.ActivityID = relatedActivityId;
                }

                this.subject.OnNext(eventEntry);
            }
            catch (Exception ex)
            {
                this.healthReporter?.ReportProblem($"Failed to write message. Error: {ex.ToString()}", TraceTag);
            }
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return this.subject.Subscribe(observer);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.subject.Dispose();
        }

        public override void Write(string message)
        {
            WriteLine(message);
        }
    }
}
