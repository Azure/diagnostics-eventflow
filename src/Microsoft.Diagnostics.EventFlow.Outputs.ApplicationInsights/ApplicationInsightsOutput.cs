// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Validation;

using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Microsoft.Diagnostics.EventFlow.Outputs.ApplicationInsights;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    public class ApplicationInsightsOutput : IOutput
    {
        private static readonly Task CompletedTask = Task.FromResult<object>(null);
        private static readonly SeverityLevel[] ToSeverityLevel = new SeverityLevel[]
        {
            SeverityLevel.Verbose, // LogLevel is not using value 0, so this should never be used
            SeverityLevel.Critical,
            SeverityLevel.Error,
            SeverityLevel.Warning,
            SeverityLevel.Information,
            SeverityLevel.Verbose
        };
        private static Lazy<Random> RandomGenerator = new Lazy<Random>();

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

                    if (e.TryGetMetadata(MetricData.MetricMetadataKind, out metadata))
                    {
                        TrackMetric(e, metadata);
                    }
                    else if (e.TryGetMetadata(RequestData.RequestMetadataKind, out metadata))
                    {
                        TrackRequest(e, metadata);
                    }
                    else if (e.TryGetMetadata(DependencyData.DependencyMetadataKind, out metadata))
                    {
                        TrackDependency(e, metadata);
                    }
                    else if (e.TryGetMetadata(ExceptionData.ExceptionMetadataKind, out metadata))
                    {
                        TrackException(e, metadata);
                    }
                    else if (e.TryGetMetadata(EventTelemetryData.EventMetadataKind, out metadata))
                    {
                        TrackAiEvent(e, metadata);
                    }
                    else
                    {
                        object message = null;
                        e.Payload.TryGetValue("Message", out message);
                        TraceTelemetry t = new TraceTelemetry(message as string ?? string.Empty);
                        t.SeverityLevel = ToSeverityLevel[(int)e.Level];
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

            if (string.IsNullOrWhiteSpace(aiOutputConfiguration.ConfigurationFilePath))
            {
                this.telemetryClient = new TelemetryClient();                
            }
            else
            {
                string configurationFileContent = File.ReadAllText(aiOutputConfiguration.ConfigurationFilePath);
                TelemetryConfiguration telemetryConfiguration = TelemetryConfiguration.CreateFromConfiguration(configurationFileContent);
                this.telemetryClient = new TelemetryClient(telemetryConfiguration);
            }

            if (!string.IsNullOrWhiteSpace(aiOutputConfiguration.InstrumentationKey))
            {
                this.telemetryClient.InstrumentationKey = aiOutputConfiguration.InstrumentationKey;
            }
        }

        private void TrackMetric(EventData e, IReadOnlyCollection<EventMetadata> metadata)
        {
            Debug.Assert(metadata != null);

            foreach (EventMetadata metricMetadata in metadata)
            {
                var result = MetricData.TryGetData(e, metricMetadata, out MetricData metricData);
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
                var result = RequestData.TryGetData(e, requestMetadata, out RequestData requestData);
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
                    rt.Timestamp = startTime.ToUniversalTime();
                }

                rt.Name = requestData.RequestName;
                rt.Success = requestData.IsSuccess;
                rt.ResponseCode = requestData.ResponseCode;

                AddProperties(rt, e, setTimestampFromEventData: false);

                telemetryClient.TrackRequest(rt);
            }
        }

        private void TrackDependency(EventData e, IReadOnlyCollection<EventMetadata> metadata)
        {
            Debug.Assert(metadata != null);

            foreach (EventMetadata dependencyMetadata in metadata)
            {
                var result = DependencyData.TryGetData(e, dependencyMetadata, out DependencyData dependencyData);
                if (result.Status != DataRetrievalStatus.Success)
                {
                    this.healthReporter.ReportWarning("ApplicationInsightsOutput: " + result.Message, EventFlowContextIdentifiers.Output);
                    continue;
                }

                var dt = new DependencyTelemetry();

                if (dependencyData.Duration != null)
                {
                    dt.Duration = dependencyData.Duration.Value;
                    // TODO: add an option to extract request start time from event data
                    DateTimeOffset startTime = e.Timestamp.Subtract(dependencyData.Duration.Value);
                    dt.Timestamp = startTime.ToUniversalTime();
                }

                dt.Success = dependencyData.IsSuccess;
                dt.ResultCode = dependencyData.ResponseCode;
                dt.Target = dependencyData.Target;
                dt.Type = dependencyData.DependencyType;

                AddProperties(dt, e, setTimestampFromEventData: false);

                telemetryClient.TrackDependency(dt);
            }
        }

        private void TrackException(EventData e, IReadOnlyCollection<EventMetadata> metadata)
        {
            Debug.Assert(metadata != null);

            foreach (EventMetadata exceptionMetadata in metadata)
            {
                var result = ExceptionData.TryGetData(e, exceptionMetadata, out ExceptionData exceptionData);
                if (result.Status != DataRetrievalStatus.Success)
                {
                    this.healthReporter.ReportWarning("ApplicationInsightsOutput: " + result.Message, EventFlowContextIdentifiers.Output);
                    continue;
                }

                var et = new ExceptionTelemetry();
                et.Exception = exceptionData.Exception;

                AddProperties(et, e);

                telemetryClient.TrackException(et);
            }
        }

        private void TrackAiEvent(EventData e, IReadOnlyCollection<EventMetadata> metadata)
        {
            Debug.Assert(metadata != null);

            foreach (EventMetadata eventMetadata in metadata)
            {
                var result = EventTelemetryData.TryGetData(e, eventMetadata, out EventTelemetryData eventData);
                if (result.Status != DataRetrievalStatus.Success)
                {
                    this.healthReporter.ReportWarning("ApplicationInsightsOutput: " + result.Message, EventFlowContextIdentifiers.Output);
                    continue;
                }

                var et = new EventTelemetry();
                et.Name = eventData.Name;

                AddProperties(et, e);

                telemetryClient.TrackEvent(et);
            }
        }

        private void AddProperties(ISupportProperties item, EventData e, bool setTimestampFromEventData = true)
        {
            ITelemetry telemetry = item as ITelemetry;
            if (telemetry != null && setTimestampFromEventData)
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
            if (propertyName == null)
            {
                return;
            }

            var properties = item.Properties;
            if (!properties.ContainsKey(propertyName))
            {
                properties.Add(propertyName, propertyValue);
                return;
            }

            string newPropertyName = propertyName + "_";
            //update property key till there is no such key in dict
            do
            {
                newPropertyName += RandomGenerator.Value.Next(0, 10);
            }
            while (properties.ContainsKey(newPropertyName));

            properties.Add(newPropertyName, propertyValue);
        }
    }
}
