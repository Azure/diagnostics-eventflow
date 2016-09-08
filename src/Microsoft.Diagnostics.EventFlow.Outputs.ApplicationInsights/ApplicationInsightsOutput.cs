// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    public class ApplicationInsightsOutput : OutputBase
    {
        private const string AppInsightsKeyName = "InstrumentationKey";

        private readonly TelemetryClient telemetryClient;

        public ApplicationInsightsOutput(IConfiguration configuration, IHealthReporter healthReporter) : base(healthReporter)
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
            if (this.telemetryClient == null || events == null || events.Count == 0)
            {
                return Task.FromResult<object>(null);
            }

            try
            {
                foreach (var e in events)
                {
                    IReadOnlyCollection<EventMetadata> metadata;
                    bool handled = false;

                    if (e.TryGetMetadata(ApplicationInsightsMetadataTypes.Metric, out metadata))
                    {
                        TrackMetric(e, metadata);
                        handled = true;
                    }

                    if (e.TryGetMetadata(ApplicationInsightsMetadataTypes.Request, out metadata))
                    {
                        TrackRequest(e, metadata);
                    }

                    if (!handled)
                    {
                        TraceTelemetry t = new TraceTelemetry(e.Payload["Message"] as string ?? string.Empty);
                        AddProperties(t, e);

                        telemetryClient.TrackTrace(t);
                    }
                }

                telemetryClient.Flush();

                this.healthReporter.ReportHealthy();
            }
            catch (Exception e)
            {
                this.healthReporter.ReportProblem("Diagnostics data upload has failed." + Environment.NewLine + e.ToString());
            }

            return Task.FromResult<object>(null);
        }

        private void TrackMetric(EventData e, IReadOnlyCollection<EventMetadata> metadata)
        {
            Debug.Assert(metadata != null);

            foreach (EventMetadata metricMetadata in metadata)
            {
                MetricTelemetry mt = new MetricTelemetry();
                mt.Name = metricMetadata["metricName"];

                double value = 0.0;
                string metricValueProperty = metricMetadata["metricValueProperty"];
                if (string.IsNullOrEmpty(metricValueProperty))
                {
                    double.TryParse(metricMetadata["metricValue"], out value);
                }
                else
                {
                    this.GetValueFromPayload<double>(e, metricValueProperty, (v) => value = v);
                }
                mt.Value = value;

                AddProperties(mt, e);

                telemetryClient.TrackMetric(mt);
            }
        }

        private void TrackRequest(EventData e, IReadOnlyCollection<EventMetadata> metadata)
        {
            Debug.Assert(metadata != null);

            foreach (EventMetadata requestMetadata in metadata)
            {
                RequestTelemetry rt = new RequestTelemetry();
                string requestName = null;
                bool? success = null;
                double duration = 0;
                // CONSIDER: add ability to send response code

                string requestNameProperty = requestMetadata["requestNameProperty"];
                Debug.Assert(!string.IsNullOrWhiteSpace(requestNameProperty));
                this.GetValueFromPayload<string>(e, requestNameProperty, (v) => requestName = v);

                this.GetValueFromPayload<bool>(e, requestMetadata["isSuccessProperty"], (v) => success = v);

                this.GetValueFromPayload<double>(e, requestMetadata["durationProperty"], (v) => duration = v);

                TimeSpan durationSpan = TimeSpan.FromMilliseconds(duration);
                DateTimeOffset startTime = e.Timestamp.Subtract(durationSpan); // TODO: add an option to extract request start time from event data

                rt.Name = requestName;
                rt.StartTime = startTime.ToUniversalTime();
                rt.Duration = durationSpan;
                rt.Success = success;

                AddProperties(rt, e);

                telemetryClient.TrackRequest(rt);
            }
        }

        private void AddProperties(ISupportProperties item, EventData e)
        {
            ITelemetry telemetry = item as ITelemetry;
            if (telemetry != null)
            {
                telemetry.Timestamp = e.Timestamp;
            }

            AddProperty(item, nameof(e.Keywords), "0x" + e.Keywords.ToString("X16"));
            AddProperty(item, nameof(e.Level), e.Level.GetName());
            AddProperty(item, nameof(e.ProviderName), e.ProviderName);

            foreach (var payloadItem in e.Payload)
            {
                AddProperty(item, payloadItem.Key, payloadItem.Value.ToString());
            }
        }

        private void AddProperty(ISupportProperties item, string propertyName, string propertyValue)
        {
            if (propertyValue != null)
            {
                item.Properties.Add(propertyName, propertyValue);
            }
        }
    }
}
