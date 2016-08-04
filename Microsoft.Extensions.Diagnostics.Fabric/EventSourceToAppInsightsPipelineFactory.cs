// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
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

            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            ConfigurationPackage configPackage = activationContext.GetConfigurationPackageObject("Config");
            string configFilePath = Path.Combine(configPackage.Path, configurationFileName);
            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException("Configuration file is missing or inaccessible", configFilePath);
            }

            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(configFilePath);
            IConfigurationRoot configurationRoot = configBuilder.Build();

            var healthReporter = new FabricHealthReporter(healthEntityName);
            var listener = ObservableEventListenerFactory.CreateListener(configurationRoot, healthReporter);
            var sender = ApplicationInsigthsSenderFactory.CreateSender(configurationRoot, healthReporter);
            if (sender == null)
            {
                listener.Dispose();
                return EmptyDisposable.Instance;
            }

            var metricMetadata = EventSourceMetadataFactory.ReadMetadata(configurationRoot, healthReporter, (esConfiguration) => esConfiguration.Metrics);
            var metricFilter = new EventMetadataFilter<MetricMetadata>(metricMetadata, healthReporter);

            var requestMetadata = EventSourceMetadataFactory.ReadMetadata(configurationRoot, healthReporter, (esConfiguration) => esConfiguration.Requests);
            var requestFilter = new EventMetadataFilter<RequestMetadata>(requestMetadata, healthReporter);

            DiagnosticsPipeline<EventData> pipeline = new DiagnosticsPipeline<EventData>(
                healthReporter,
                new IObservable<EventData>[] { listener },
                new EventSink<EventData>[] { new EventSink<EventData>(sender, new IEventFilter<EventData>[] { metricFilter, requestFilter } )});

            return pipeline;
        }
    }
}
