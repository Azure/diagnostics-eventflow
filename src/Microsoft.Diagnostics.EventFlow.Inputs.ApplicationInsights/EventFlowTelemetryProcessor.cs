// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Validation;
using Microsoft.Diagnostics.EventFlow.Inputs;

namespace Microsoft.Diagnostics.EventFlow.ApplicationInsights
{
    public class EventFlowTelemetryProcessor : ITelemetryProcessor
    {
        private static readonly LogLevel[] ToLogLevel = new LogLevel[]
        {
            LogLevel.Verbose,
            LogLevel.Informational,
            LogLevel.Warning,
            LogLevel.Error,
            LogLevel.Critical
        };
        public static readonly string TelemetryTypeProperty = "TelemetryType";

        private ITelemetryProcessor next;
        private IObserver<EventData> input;
        private IHealthReporter healthReporter;

        public EventFlowTelemetryProcessor(ITelemetryProcessor next)
        {
            Requires.NotNull(next, nameof(next));

            this.next = next;
        }

        public DiagnosticPipeline Pipeline
        {
            set
            {
                Requires.NotNull(value, nameof(value));

                this.input = value.Inputs.OfType<IItemWithLabels>()
                    .Where(i => i.Labels.ContainsKey(ApplicationInsightsInputFactory.ApplicationInsightsInputTag))
                    .Cast<IObserver<EventData>>().FirstOrDefault();
                this.healthReporter = value.HealthReporter;
            }
        }

        public void Process(ITelemetry item)
        {
            var currentInput = this.input;

            if (currentInput == null)
            {
                this.next.Process(item);
                return;
            }

            try
            {
                item.Sanitize();

                var eventData = new EventData();
                eventData.Timestamp = item.Timestamp;
                eventData.ProviderName = "EventFlow-ApplicationInsightsInput";

                var eventPayload = eventData.Payload;

                if (item is RequestTelemetry)
                {
                    var request = item as RequestTelemetry;
                    eventData.Level = LogLevel.Informational;
                    AddRequestProperties(eventPayload, request);
                    AddMetricValues(eventData, request.Metrics);
                }
                else if (item is TraceTelemetry)
                {
                    var trace = item as TraceTelemetry;
                    if (trace.SeverityLevel.HasValue)
                    {
                        eventData.Level = ToLogLevel[(int)trace.SeverityLevel.Value];
                    }
                    else
                    {
                        eventData.Level = LogLevel.Verbose;
                    }
                    AddTraceProperties(eventPayload, trace);
                }
                else if (item is EventTelemetry)
                {
                    var evt = item as EventTelemetry;
                    eventData.Level = LogLevel.Informational;
                    AddEventProperties(eventPayload, evt);
                    AddMetricValues(eventData, evt.Metrics);
                }
                else if (item is DependencyTelemetry)
                {
                    var dependencyCall = item as DependencyTelemetry;
                    eventData.Level = LogLevel.Informational;
                    AddDependencyProperties(eventPayload, dependencyCall);
                    AddMetricValues(eventData, dependencyCall.Metrics);
                }
                else if (item is MetricTelemetry)
                {
                    var metric = item as MetricTelemetry;
                    eventData.Level = LogLevel.Informational;
                    AddMetricProperties(eventPayload, metric);
                }
                else if (item is ExceptionTelemetry)
                {
                    var exception = item as ExceptionTelemetry;
                    if (exception.SeverityLevel.HasValue)
                    {
                        eventData.Level = ToLogLevel[(int)exception.SeverityLevel.Value];
                    }
                    else
                    {
                        eventData.Level = LogLevel.Warning;
                    }
                    AddExceptionProperties(eventPayload, exception);
                    AddMetricValues(eventData, exception.Metrics);
                }
                else if (item is PageViewTelemetry)
                {
                    var pageView = item as PageViewTelemetry;
                    eventData.Level = LogLevel.Informational;
                    AddPageViewProperties(eventPayload, pageView);
                    AddMetricValues(eventData, pageView.Metrics);
                }
                else if (item is AvailabilityTelemetry)
                {
                    var availability = item as AvailabilityTelemetry;
                    eventData.Level = availability.Success ? LogLevel.Informational : LogLevel.Warning;
                    AddAvailabilityProperties(eventPayload, availability);
                    AddMetricValues(eventData, availability.Metrics);
                }

                var itemWithProperties = item as ISupportProperties;
                if (itemWithProperties != null)
                {
                    foreach (var property in itemWithProperties.Properties)
                    {
                        AddPayloadProperty(eventData, property.Key, property.Value);
                    }
                }

                AddContextProperties(eventData, item.Context);

                // TODO: also add values from ISupportMetrics when that interface becomes public

                currentInput.OnNext(eventData);
            }
            finally
            {
                this.next.Process(item);
            }
        }

        private void AddContextProperties(EventData eventData, TelemetryContext context)
        {
            if (context.Component != null && !string.IsNullOrEmpty(context.Component.Version))
            {
                AddPayloadProperty(eventData, "ai_component_version", context.Component.Version);
            }

            var deviceContext = context.Device;
            if (deviceContext != null)
            {
                const string deviceContextPrefix = "ai_device_";
                if (!string.IsNullOrEmpty(deviceContext.Type))
                {
                    AddPayloadProperty(eventData, deviceContextPrefix + "type", deviceContext.Type);
                }
                if (!string.IsNullOrEmpty(deviceContext.Id))
                {
                    AddPayloadProperty(eventData, deviceContextPrefix + "id", deviceContext.Id);
                }
                if (!string.IsNullOrEmpty(deviceContext.OperatingSystem))
                {
                    AddPayloadProperty(eventData, deviceContextPrefix + "operating_system", deviceContext.OperatingSystem);
                }
                if (!string.IsNullOrEmpty(deviceContext.OemName))
                {
                    AddPayloadProperty(eventData, deviceContextPrefix + "oem_name", deviceContext.OemName);
                }
                if (!string.IsNullOrEmpty(deviceContext.Model))
                {
                    AddPayloadProperty(eventData, deviceContextPrefix + "model", deviceContext.Model);
                }
            }

            var cloudContext = context.Cloud;
            if (context.Cloud != null)
            {
                const string cloudContextPrefix = "ai_cloud_";
                if (!string.IsNullOrEmpty(cloudContext.RoleName))
                {
                    AddPayloadProperty(eventData, cloudContextPrefix + "role_name", cloudContext.RoleName);
                }
                if (!string.IsNullOrEmpty(cloudContext.RoleInstance))
                {
                    AddPayloadProperty(eventData, cloudContextPrefix + "role_instance", cloudContext.RoleInstance);
                }
            }

            var sessionContext = context.Session;
            if (sessionContext != null)
            {
                const string sessionContextPrefix = "ai_session_";
                if (!string.IsNullOrEmpty(sessionContext.Id))
                {
                    AddPayloadProperty(eventData, sessionContextPrefix + "id", sessionContext.Id);
                }
                if (sessionContext.IsFirst.HasValue)
                {
                    AddPayloadProperty(eventData, sessionContextPrefix + "is_first", sessionContext.IsFirst.Value);
                }
            }

            var userContext = context.User;
            if (userContext != null)
            {
                const string userContextPrefix = "ai_user_";
                if (!string.IsNullOrEmpty(userContext.Id))
                {
                    AddPayloadProperty(eventData, userContextPrefix + "id", userContext.Id);
                }
                if (!string.IsNullOrEmpty(userContext.AccountId))
                {
                    AddPayloadProperty(eventData, userContextPrefix + "account_id", userContext.AccountId);
                }
                if (!string.IsNullOrEmpty(userContext.UserAgent))
                {
                    AddPayloadProperty(eventData, userContextPrefix + "user_agent", userContext.UserAgent);
                }
                if (!string.IsNullOrEmpty(userContext.AuthenticatedUserId))
                {
                    AddPayloadProperty(eventData, userContextPrefix + "authenticated_user_id", userContext.AuthenticatedUserId);
                }
            }

            var operationContext = context.Operation;
            if (operationContext != null)
            {
                const string operationContextPrefix = "ai_operation_";
                if (!string.IsNullOrEmpty(operationContext.Id))
                {
                    AddPayloadProperty(eventData, operationContextPrefix + "id", operationContext.Id);
                }
                if (!string.IsNullOrEmpty(operationContext.ParentId))
                {
                    AddPayloadProperty(eventData, operationContextPrefix + "parent_id", operationContext.ParentId);
                }
                if (!string.IsNullOrEmpty(operationContext.CorrelationVector))
                {
                    AddPayloadProperty(eventData, operationContextPrefix + "correlation_vector", operationContext.CorrelationVector);
                }
                if (!string.IsNullOrEmpty(operationContext.Name))
                {
                    AddPayloadProperty(eventData, operationContextPrefix + "name", operationContext.Name);
                }
                if (!string.IsNullOrEmpty(operationContext.SyntheticSource))
                {
                    AddPayloadProperty(eventData, operationContextPrefix + "synthetic_source", operationContext.SyntheticSource);
                }
            }

            if (context.Location != null && !string.IsNullOrEmpty(context.Location.Ip))
            {
                AddPayloadProperty(eventData, "ai_location_ip", context.Location.Ip);
            }

            if (context.Properties != null)
            {
                foreach(var property in context.Properties)
                {
                    AddPayloadProperty(eventData, "ai_" + property.Key, property.Value);
                }
            }
        }

        private void AddRequestProperties(IDictionary<string, object> eventPayload, RequestTelemetry request)
        {
            eventPayload.Add(TelemetryTypeProperty, "request");
            eventPayload.Add(nameof(request.Name), request.Name);
            eventPayload.Add(nameof(request.Duration), request.Duration);
            if (!string.IsNullOrEmpty(request.Id))
            {
                eventPayload.Add(nameof(request.Id), request.Id);
            }
            if (!string.IsNullOrEmpty(request.ResponseCode))
            {
                eventPayload.Add(nameof(request.ResponseCode), request.ResponseCode);
            }
            if (request.Success.HasValue)
            {
                eventPayload.Add(nameof(request.Success), request.Success.Value);
            }
            if (request.Url != null)
            {
                eventPayload.Add(nameof(request.Url), request.Url.ToString());
            }
            if (!string.IsNullOrEmpty(request.Source))
            {
                eventPayload.Add(nameof(request.Source), request.Source);
            }
        }

        private void AddTraceProperties(IDictionary<string, object>  eventPayload, TraceTelemetry trace)
        {
            eventPayload.Add(TelemetryTypeProperty, "trace");
            eventPayload.Add(nameof(trace.Message), trace.Message);
        }

        private void AddEventProperties(IDictionary<string, object> eventPayload, EventTelemetry evt)
        {
            eventPayload.Add(TelemetryTypeProperty, "event");
            eventPayload.Add(nameof(evt.Name), evt.Name);
        }

        private void AddDependencyProperties(IDictionary<string, object> eventPayload, DependencyTelemetry dependencyCall)
        {
            eventPayload.Add(TelemetryTypeProperty, "dependency");
            eventPayload.Add(nameof(dependencyCall.Name), dependencyCall.Name);
            if (dependencyCall.Success.HasValue)
            {
                eventPayload.Add(nameof(dependencyCall.Success), dependencyCall.Success.Value);
            }
            if (dependencyCall.Duration != TimeSpan.Zero)
            {
                eventPayload.Add(nameof(dependencyCall.Duration), dependencyCall.Duration);
            }
            if (!string.IsNullOrEmpty(dependencyCall.Type))
            {
                eventPayload.Add(nameof(dependencyCall.Type), dependencyCall.Type);
            }
            if (!string.IsNullOrEmpty(dependencyCall.Target))
            {
                eventPayload.Add(nameof(dependencyCall.Target), dependencyCall.Target);
            }
            if (!string.IsNullOrEmpty(dependencyCall.Data))
            {
                eventPayload.Add(nameof(dependencyCall.Data), dependencyCall.Data);
            }
            if (!string.IsNullOrEmpty(dependencyCall.ResultCode))
            {
                eventPayload.Add(nameof(dependencyCall.ResultCode), dependencyCall.ResultCode);
            }
            if (!string.IsNullOrEmpty(dependencyCall.Id))
            {
                eventPayload.Add(nameof(dependencyCall.Id), dependencyCall.Id);
            }
        }

        private void AddMetricProperties(IDictionary<string, object> eventPayload, MetricTelemetry metric)
        {
            eventPayload.Add(TelemetryTypeProperty, "metric");
            eventPayload.Add(nameof(metric.Name), metric.Name);
            eventPayload.Add(nameof(metric.Sum), metric.Sum);
            if (metric.Count.HasValue)
            {
                eventPayload.Add(nameof(metric.Count), metric.Count.Value);
            }
            if (metric.Min.HasValue)
            {
                eventPayload.Add(nameof(metric.Min), metric.Min.Value);
            }
            if (metric.Max.HasValue)
            {
                eventPayload.Add(nameof(metric.Max), metric.Max.Value);
            }
            if (metric.StandardDeviation.HasValue)
            {
                eventPayload.Add(nameof(metric.StandardDeviation), metric.StandardDeviation.Value);
            }
        }

        private void AddExceptionProperties(IDictionary<string, object> eventPayload, ExceptionTelemetry exception)
        {
            eventPayload.Add(TelemetryTypeProperty, "exception");
            eventPayload.Add(nameof(exception.Exception), exception.Exception.ToString());
            if (!string.IsNullOrEmpty(exception.Message))
            {
                eventPayload.Add(nameof(exception.Message), exception.Message);
            }
        }

        private void AddPageViewProperties(IDictionary<string, object> eventPayload, PageViewTelemetry pageView)
        {
            eventPayload.Add(TelemetryTypeProperty, "page_view");
            eventPayload.Add(nameof(pageView.Name), pageView.Name);
            if (pageView.Url != null)
            {
                eventPayload.Add(nameof(pageView.Url), pageView.Url.ToString());
            }
            if (pageView.Duration != TimeSpan.Zero)
            {
                eventPayload.Add(nameof(pageView.Duration), pageView.Duration);
            }
        }

        private void AddAvailabilityProperties(IDictionary<string, object> eventPayload, AvailabilityTelemetry availability)
        {
            eventPayload.Add(TelemetryTypeProperty, "availability");
            eventPayload.Add(nameof(availability.Name), availability.Name);
            if (!string.IsNullOrEmpty(availability.Id))
            {
                eventPayload.Add(nameof(availability.Id), availability.Id);
            }
            if (availability.Duration != TimeSpan.Zero)
            {
                eventPayload.Add(nameof(availability.Duration), availability.Duration);
            }
            eventPayload.Add(nameof(availability.Success), availability.Success);
            if (!string.IsNullOrEmpty(availability.RunLocation))
            {
                eventPayload.Add(nameof(availability.RunLocation), availability.RunLocation);
            }
            if (!string.IsNullOrEmpty(availability.Message))
            {
                eventPayload.Add(nameof(availability.Message), availability.Message);
            }
        }

        private void AddMetricValues(EventData eventData, IDictionary<string, double> metricSet)
        {
            foreach (var metric in metricSet)
            {
                AddPayloadProperty(eventData, metric.Key, metric.Value);
            }
        }

        private void AddPayloadProperty(EventData eventData, string key, object value)
        {
            eventData.AddPayloadProperty(key, value, this.healthReporter, nameof(EventFlowTelemetryProcessor));
        }
    }
}
