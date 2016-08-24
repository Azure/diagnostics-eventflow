// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Inputs
{
    public class TraceInput : DefaultTraceListener, IObservable<EventData>, IDisposable
    {
        public static readonly string TraceTag = nameof(TraceInput);

        private SimpleSubject<EventData> subject;
        private readonly IHealthReporter healthReporter;
        public TraceInput(IConfigurationSection configuration, IHealthReporter healthReporter)
        {
            Validation.Assumes.True("Trace".Equals(configuration["type"], StringComparison.Ordinal), "Invalid trace configuration.");
            Validation.Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;

            subject = new SimpleSubject<EventData>();

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

        public override void WriteLine(string message)
        {
            try
            {
                EventData data = new EventData()
                {
                    ProviderName = nameof(TraceInput),
                    Message = message
                };
                this.subject.OnNext(data);
            }
            catch (Exception ex)
            {
                this.healthReporter.ReportProblem($"Fail to write message. Error: {ex.ToString()}", TraceTag);
            }
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return subject.Subscribe(observer);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            subject.Dispose();
        }
    }
}
