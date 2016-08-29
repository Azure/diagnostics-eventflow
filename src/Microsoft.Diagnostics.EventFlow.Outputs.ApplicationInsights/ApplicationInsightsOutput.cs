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
                    EventMetadata singleMetadata;
                    IEnumerable<EventMetadata> multiMetadata;
                    bool handled = false;

                    if (e.TryGetMetadata(ApplicationInsightsMetadataTypes.Metric, out singleMetadata, out multiMetadata))
                    {
                        TrackMetric(e, singleMetadata, multiMetadata);
                        handled = true;
                    }

                    if (e.TryGetMetadata(ApplicationInsightsMetadataTypes.Request, out singleMetadata, out multiMetadata))
                    {
                        TrackRequest(e, singleMetadata, multiMetadata);
                    }

                    if (!handled)
                    {
                        TraceTelemetry t = new TraceTelemetry(e.Message ?? string.Empty);
                        AddProperties(t, e);

                        telemetryClient.TrackTrace(t);
                    }
                }

                telemetryClient.Flush();

                this.ReportHealthy();
            }
            catch (Exception e)
            {
                this.ReportProblem("Diagnostics data upload has failed." + Environment.NewLine + e.ToString());
            }

            return Task.FromResult<object>(null);
        }

        private void TrackMetric(EventData e, EventMetadata singleMetadata, IEnumerable<EventMetadata> multiMetadata)
        {
            Debug.Assert(singleMetadata != null || multiMetadata != null);

            if (singleMetadata == null)
            {
                // TODO: handle multi-metadata case
                return;
            }

            MetricTelemetry mt = new MetricTelemetry();
            mt.Name = singleMetadata["metricName"];

            double value = 0.0;
            string metricValueProperty = singleMetadata["metricValueProperty"];
            if (string.IsNullOrEmpty(metricValueProperty))
            {
                double.TryParse(singleMetadata["metricValue"], out value);
            }
            else
            {
                this.GetValueFromPayload<double>(e, metricValueProperty, (v) => value = v);
            }
            mt.Value = value;

            AddProperties(mt, e);

            telemetryClient.TrackMetric(mt);
        }

        private void TrackRequest(EventData e, EventMetadata singleMetadata, IEnumerable<EventMetadata> multiMetadata)
        {
            RequestTelemetry rt = new RequestTelemetry();

            Debug.Assert(singleMetadata != null || multiMetadata != null);

            if (singleMetadata == null)
            {
                // TODO: handle multi-metadata case
                return;
            }

            string requestName = null;
            bool? success = null;
            double duration = 0;
            // CONSIDER: add ability to send response code

            string requestNameProperty = singleMetadata["requestNameProperty"];
            Debug.Assert(!string.IsNullOrWhiteSpace(requestNameProperty));
            this.GetValueFromPayload<string>(e, requestNameProperty, (v) => requestName = v);

            this.GetValueFromPayload<bool>(e, singleMetadata["isSuccessProperty"], (v) => success = v);

            this.GetValueFromPayload<double>(e, singleMetadata["durationProperty"], (v) => duration = v);

            TimeSpan durationSpan = TimeSpan.FromMilliseconds(duration);
            DateTimeOffset startTime = e.Timestamp.Subtract(durationSpan); // TODO: add an option to extract request start time from event data

            rt.Name = requestName;
            rt.StartTime = startTime.ToUniversalTime();
            rt.Duration = durationSpan;
            rt.Success = success;

            AddProperties(rt, e);

            telemetryClient.TrackRequest(rt);
        }

        private void AddProperties(ISupportProperties item, EventData e)
        {
            ITelemetry telemetry = item as ITelemetry;
            if (telemetry != null)
            {
                telemetry.Timestamp = e.Timestamp;
            }

            if (e.EventId != 0)
            {
                AddProperty(item, nameof(e.EventId), e.EventId.ToString());
            }
            AddProperty(item, nameof(e.EventName), e.EventName);
            AddProperty(item, nameof(e.Keywords), e.Keywords);
            AddProperty(item, nameof(e.Level), e.Level);
            AddProperty(item, nameof(e.Message), e.Message);
            AddProperty(item, nameof(e.ProviderName), e.ProviderName);
            AddProperty(item, nameof(e.ActivityID), e.ActivityID);

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
