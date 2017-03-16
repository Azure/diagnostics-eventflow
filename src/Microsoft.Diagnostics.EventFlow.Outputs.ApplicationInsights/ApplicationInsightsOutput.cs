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
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    public class ApplicationInsightsOutput : IOutput
    {
        private static readonly Task CompletedTask = Task.FromResult<object>(null);

        private TelemetryClient telemetryClient;
        private readonly IHealthReporter healthReporter;

        public ApplicationInsightsOutput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            var aiOutputConfiguration = new ApplicationInsightsOutputConfiguration();
            try
            {
                configuration.Bind(aiOutputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(ApplicationInsightsOutput)} configuration encountered: '{configuration.ToString()}'",
                    EventFlowContextIdentifiers.Configuration);
                throw;
            }

            Initialize(aiOutputConfiguration);
        }

        public ApplicationInsightsOutput(ApplicationInsightsOutputConfiguration applicationInsightsOutputConfiguration, IHealthReporter healthReporter)
        {
            Requires.NotNull(applicationInsightsOutputConfiguration, nameof(applicationInsightsOutputConfiguration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            Initialize(applicationInsightsOutputConfiguration);
        }

        public Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (this.telemetryClient == null || events == null || events.Count == 0)
            {
                return CompletedTask;
            }

            try
            {
                foreach (var e in events)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return CompletedTask;
                    }

                    IReadOnlyCollection<EventMetadata> metadata;
                    bool handled = false;

                    if (e.TryGetMetadata(MetricData.MetricMetadataKind, out metadata))
                    {
                        TrackMetric(e, metadata);
                        handled = true;
                    }

                    if (e.TryGetMetadata(RequestData.RequestMetadataKind, out metadata))
                    {
                        TrackRequest(e, metadata);
                        handled = true;
                    }

                    if (!handled)
                    {
                        object message = null;
                        e.Payload.TryGetValue("Message", out message);
                        TraceTelemetry t = new TraceTelemetry(message as string ?? string.Empty);
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

            return CompletedTask;
        }

        private void Initialize(ApplicationInsightsOutputConfiguration aiOutputConfiguration)
        {
            Debug.Assert(aiOutputConfiguration != null);
            Debug.Assert(this.healthReporter != null);

            if (string.IsNullOrWhiteSpace(aiOutputConfiguration.InstrumentationKey))
            {
                string errorMessage = $"{nameof(ApplicationInsightsOutput)}: Application Insights instrumentation key is is not set)";
                this.healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }

            this.telemetryClient = new TelemetryClient();
            this.telemetryClient.InstrumentationKey = aiOutputConfiguration.InstrumentationKey;
        }

        private void TrackMetric(EventData e, IReadOnlyCollection<EventMetadata> metadata)
        {
            Debug.Assert(metadata != null);

            foreach (EventMetadata metricMetadata in metadata)
            {
                MetricData metricData;
                var result = MetricData.TryGetData(e, metricMetadata, out metricData);
                if (result.Status != DataRetrievalStatus.Success)
                {
                    this.healthReporter.ReportWarning("ApplicationInsightsOutput: " + result.Message, EventFlowContextIdentifiers.Output);
                    continue;
                }

                MetricTelemetry mt = new MetricTelemetry();
                mt.Name = metricData.MetricName;
                mt.Value = metricData.Value;
                AddProperties(mt, e);
                telemetryClient.TrackMetric(mt);
            }
        }

        private void TrackRequest(EventData e, IReadOnlyCollection<EventMetadata> metadata)
        {
            Debug.Assert(metadata != null);

            foreach (EventMetadata requestMetadata in metadata)
            {
                RequestData requestData;
                var result = RequestData.TryGetData(e, requestMetadata, out requestData);
                if (result.Status != DataRetrievalStatus.Success)
                {
                    this.healthReporter.ReportWarning("ApplicationInsightsOutput: " + result.Message, EventFlowContextIdentifiers.Output);
                    continue;
                }

                RequestTelemetry rt = new RequestTelemetry();

                if (requestData.Duration != null)
                {
                    rt.Duration = requestData.Duration.Value;
                    // TODO: add an option to extract request start time from event data
                    DateTimeOffset startTime = e.Timestamp.Subtract(requestData.Duration.Value);
                    rt.StartTime = startTime.ToUniversalTime();
                }

                rt.Name = requestData.RequestName;
                rt.Success = requestData.IsSuccess;
                rt.ResponseCode = requestData.ResponseCode;

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
                AddProperty(item, payloadItem.Key, payloadItem.Value?.ToString());
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
