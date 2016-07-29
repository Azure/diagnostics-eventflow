// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventListeners
{
    public class ApplicationInsightsSender : SenderBase<EventData>
    {
        private const string AppInsightsKeyName = "InstrumentationKey";

        private readonly TelemetryClient telemetryClient;

        public ApplicationInsightsSender(IConfiguration configuration, IHealthReporter healthReporter): base(healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            string telemetryKey = configuration[AppInsightsKeyName];
            if (string.IsNullOrWhiteSpace(telemetryKey))
            {
                healthReporter.ReportProblem($"ApplicationInsightsSender is missing required configuration ('{AppInsightsKeyName}' value is not set)");
                return;
            }

            this.telemetryClient = new TelemetryClient();
            this.telemetryClient.InstrumentationKey = telemetryKey;
        }

        public override Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            // TODO: transmit all event properties
            // TODO: support higher-level AI concepts like metrics, dependency calls, and requests

            if (this.telemetryClient == null || events == null || events.Count == 0)
            {
                return Task.CompletedTask;
            }

            try
            {
                foreach (var e in events)
                {
                    var properties = new Dictionary<string, string>
                    {
                        {nameof(e.EventName), e.EventName },
                        {nameof(e.EventId), e.EventId.ToString() },
                        {nameof(e.Keywords), e.Keywords},
                        {nameof(e.Level), e.Level},
                        {nameof(e.Message), e.Message},
                        {nameof(e.ProviderName), e.ProviderName}
                    };


                    foreach (var item in e.Payload)
                    {
                        properties.Add(item.Key, item.Value.ToString());
                    }

                    telemetryClient.TrackTrace(e.Message, properties);
                }

                telemetryClient.Flush();

                this.ReportSenderHealthy();
            }
            catch (Exception e)
            {
                this.ReportSenderProblem("Diagnostics data upload has failed." + Environment.NewLine + e.ToString());
            }

            return Task.CompletedTask;
        }
    }
}
