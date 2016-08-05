// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics;
using Microsoft.Extensions.Diagnostics.Metadata;
using Validation;

namespace Microsoft.Extensions.Diagnostics.Fabric
{
    public static class EventSourceToAppInsightsPipelineFactory 
    {
        public static IDisposable CreatePipeline(string healthEntityName, string configurationFileName = "Diagnostics.json")
        {
            // TODO: dynamically re-configure the pipeline when configuration changes, without stopping the service

            Requires.NotNullOrWhiteSpace(healthEntityName, nameof(healthEntityName));

            var healthReporter = new FabricHealthReporter(healthEntityName);

            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            ConfigurationPackage configPackage = activationContext.GetConfigurationPackageObject("Config");
            string configFilePath = Path.Combine(configPackage.Path, configurationFileName);
            if (!File.Exists(configFilePath))
            {
                healthReporter.ReportProblem($"{nameof(EventSourceToAppInsightsPipelineFactory)}: configuration file '{configFilePath}' is missing or inaccessible");
                return EmptyDisposable.Instance;
            }

            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(configFilePath);
            IConfigurationRoot configurationRoot = configBuilder.Build();

            var listeners = new List<IObservable<EventData>>();
            var eventListener = ObservableEventListenerFactory.CreateListener(configurationRoot, healthReporter);
            if (eventListener == null)
            {
                healthReporter.ReportProblem($"{nameof(EventSourceToAppInsightsPipelineFactory)}: could not create event listener-configuration might be invalid");
                return EmptyDisposable.Instance;
            }
            listeners.Add(eventListener);

            var performanceCounterListener = PerformanceCounterListenerFactory.CreateListener(configurationRoot, healthReporter);
            if (performanceCounterListener != null)
            {
                listeners.Add(performanceCounterListener);
            }

            var sender = ApplicationInsigthsSenderFactory.CreateSender(configurationRoot, healthReporter);
            if (sender == null)
            {
                healthReporter.ReportProblem($"{nameof(EventSourceToAppInsightsPipelineFactory)}: could not create Application Insights event sender");
                eventListener.Dispose();
                return EmptyDisposable.Instance;
            }

            var metricMetadata = EventSourceMetadataFactory.ReadMetadata(configurationRoot, healthReporter, (esConfiguration) => esConfiguration.Metrics);
            var metricFilter = new EventMetadataFilter<EventMetricMetadata>(metricMetadata, MetadataKind.Metric);

            var requestMetadata = EventSourceMetadataFactory.ReadMetadata(configurationRoot, healthReporter, (esConfiguration) => esConfiguration.Requests);
            var requestFilter = new EventMetadataFilter<RequestMetadata>(requestMetadata, MetadataKind.Request);

            DiagnosticsPipeline<EventData> pipeline = new DiagnosticsPipeline<EventData>(
                healthReporter,
                listeners,
                new EventSink<EventData>[] { new EventSink<EventData>(sender, new IEventFilter<EventData>[] { metricFilter, requestFilter } )});

            return pipeline;
        }
    }
}
