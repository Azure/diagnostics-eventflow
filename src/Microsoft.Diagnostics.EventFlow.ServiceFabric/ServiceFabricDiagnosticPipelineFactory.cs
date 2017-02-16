// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.ServiceFabric
{
    public static class ServiceFabricDiagnosticPipelineFactory
    {
        public static readonly string ConfigurationPackageName = "Config";
        public static readonly string FabricConfigurationValueReference = @"servicefabric:/(?<section>\w+)/(?<name>\w+)";

        public static DiagnosticPipeline CreatePipeline(string healthEntityName, string configurationFileName = "eventFlowConfig.json")
        {
            // TODO: dynamically re-configure the pipeline when configuration changes, without stopping the service

            Requires.NotNullOrWhiteSpace(healthEntityName, nameof(healthEntityName));

            var healthReporter = new ServiceFabricHealthReporter(healthEntityName);

            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            ConfigurationPackage configPackage = activationContext.GetConfigurationPackageObject(ConfigurationPackageName);
            string configFilePath = Path.Combine(configPackage.Path, configurationFileName);
            if (!File.Exists(configFilePath))
            {
                string errorMessage = $"{nameof(ServiceFabricDiagnosticPipelineFactory)}: configuration file '{configFilePath}' is missing or inaccessible";
                healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }

            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(configFilePath);
            configBuilder.AddServiceFabric(ConfigurationPackageName);
            IConfigurationRoot configurationRoot = configBuilder.Build().ApplyFabricConfigurationOverrides(healthReporter);

            return DiagnosticPipelineFactory.CreatePipeline(configurationRoot, new ServiceFabricHealthReporter(healthEntityName));
        }

        internal static IConfigurationRoot ApplyFabricConfigurationOverrides(this IConfigurationRoot configurationRoot, IHealthReporter healthReporter)
        {
            Debug.Assert(configurationRoot != null);
            Debug.Assert(healthReporter != null);

            Regex fabricValueReferenceRegex = new Regex(FabricConfigurationValueReference, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

            // Use ToList() to ensure that configuration is fully enumerated before starting to modify it.
            foreach (var kvp in configurationRoot.AsEnumerable().ToList())
            {
                if (kvp.Value == null)
                {
                    continue;
                }

                try
                {
                    Match valueReferenceMatch = fabricValueReferenceRegex.Match(kvp.Value);
                    if (valueReferenceMatch.Success)
                    {
                        string valueReferencePath = ConfigurationPath.Combine(valueReferenceMatch.Groups["section"].Value, valueReferenceMatch.Groups["name"].Value);
                        string newValue = configurationRoot[valueReferencePath];
                        if (string.IsNullOrEmpty(newValue))
                        {
                            healthReporter.ReportWarning(
                                $"Configuration value reference '{kvp.Value}' was encountered but no corresponding configuration value was found using path '{valueReferencePath}'", 
                                EventFlowContextIdentifiers.Configuration);
                        }
                        else
                        {
                            configurationRoot[kvp.Key] = newValue;
                        }
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    continue;
                }
            }

            return configurationRoot;
        }
    }
}
