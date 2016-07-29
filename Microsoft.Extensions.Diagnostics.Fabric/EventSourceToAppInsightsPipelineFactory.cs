// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Fabric;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Fabric
{
    public static class EventSourceToAppInsightsPipelineFactory 
    {
        IDisposable CreatePipeline(string configurationFileName = "Diagnostics.json")
        {
            // TODO: dynamically re-configure the pipeline when configuration changes, without stopping the service

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

            DiagnosticsPipeline<EventData> pipeline = new DiagnosticsPipeline<EventData>)(
                    
                );
        }
    }
}
