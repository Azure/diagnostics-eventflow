// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Extensions.Diagnostics
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
            // TODO: support higher-level AI concepts like metrics, dependency calls, and requests

            if (this.telemetryClient == null || events == null || events.Count == 0)
            {
                return Task.CompletedTask;
            }

            try
            {
                foreach (var e in events)
                {
                    TraceTelemetry t = new TraceTelemetry(e.Message ?? string.Empty);
                    AddProperties(t, e);

                    telemetryClient.TrackTrace(t);
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

        private void AddProperties(ISupportProperties item, EventData e)
        {
            ITelemetry telemetry = item as ITelemetry;
            if (telemetry != null)
            {
                telemetry.Timestamp = e.Timestamp;
            }

            item.Properties.Add(nameof(e.EventId), e.EventId.ToString());
            item.Properties.Add(nameof(e.EventName), e.EventName);
            item.Properties.Add(nameof(e.Keywords), e.Keywords);
            item.Properties.Add(nameof(e.Level), e.Level);
            item.Properties.Add(nameof(e.Message), e.Message);
            item.Properties.Add(nameof(e.ProviderName), e.ProviderName);
            item.Properties.Add(nameof(e.ActivityID), e.ActivityID);

            foreach (var payloadItem in e.Payload)
            {
                item.Properties.Add(payloadItem.Key, payloadItem.Value.ToString());
            }
        }
    }
}
