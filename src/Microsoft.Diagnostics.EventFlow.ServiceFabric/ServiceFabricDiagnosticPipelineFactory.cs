// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Fabric;
using System.IO;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.ServiceFabric
{
    public static class ServiceFabricDiagnosticPipelineFactory
    {
        public static DiagnosticPipeline CreatePipeline(string healthEntityName, string configurationFileName = "eventFlowConfig.json")
        {
            // TODO: dynamically re-configure the pipeline when configuration changes, without stopping the service

            Requires.NotNullOrWhiteSpace(healthEntityName, nameof(healthEntityName));

            var healthReporter = new ServiceFabricHealthReporter(healthEntityName);

            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            ConfigurationPackage configPackage = activationContext.GetConfigurationPackageObject("Config");
            string configFilePath = Path.Combine(configPackage.Path, configurationFileName);
            if (!File.Exists(configFilePath))
            {
                string errorMessage = $"{nameof(ServiceFabricDiagnosticPipelineFactory)}: configuration file '{configFilePath}' is missing or inaccessible";
                healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }

            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(configFilePath);
            IConfigurationRoot configurationRoot = configBuilder.Build();
            return DiagnosticPipelineFactory.CreatePipeline(configurationRoot);
        }
    }
}
