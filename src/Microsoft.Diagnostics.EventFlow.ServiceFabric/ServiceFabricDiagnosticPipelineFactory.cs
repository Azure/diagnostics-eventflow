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
using Microsoft.Extensions.Configuration.ServiceFabric;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.ServiceFabric
{
    public static class ServiceFabricDiagnosticPipelineFactory
    {
        public static readonly string FabricConfigurationValueReference = @"servicefabric:/(?<section>\w+)/(?<name>\w+)";
        public static readonly string FabricConfigurationFileReference = @"servicefabricfile:/(?<filename>.+)";

        public static DiagnosticPipeline CreatePipeline(
            string healthEntityName, 
            string configurationFileName = "eventFlowConfig.json",
            string configurationPackageName = ServiceFabricConfigurationProvider.DefaultConfigurationPackageName)
        {
            // TODO: dynamically re-configure the pipeline when configuration changes, without stopping the service

            Requires.NotNullOrWhiteSpace(healthEntityName, nameof(healthEntityName));
            Requires.NotNullOrWhiteSpace(configurationFileName, nameof(configurationFileName));
            Requires.NotNullOrWhiteSpace(configurationPackageName, nameof(configurationPackageName));

            var healthReporter = new ServiceFabricHealthReporter(healthEntityName);

            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            ConfigurationPackage configPackage;
            try
            {
                configPackage = activationContext.GetConfigurationPackageObject(configurationPackageName);
            }
            catch
            {
                string errorMessage = $"{nameof(ServiceFabricDiagnosticPipelineFactory)}: configuration package '{configurationPackageName}' is missing or inaccessible";
                healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw;
            }

            string configFilePath = Path.Combine(configPackage.Path, configurationFileName);
            if (!File.Exists(configFilePath))
            {
                string errorMessage = $"{nameof(ServiceFabricDiagnosticPipelineFactory)}: configuration file '{configFilePath}' is missing or inaccessible";
                healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Configuration);
                throw new Exception(errorMessage);
            }

            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(configFilePath);
            configBuilder.AddServiceFabric(ServiceFabricConfigurationProvider.DefaultConfigurationPackageName);
            IConfigurationRoot configurationRoot = configBuilder.Build().ApplyFabricConfigurationOverrides(configPackage.Path, healthReporter);

            return DiagnosticPipelineFactory.CreatePipeline(configurationRoot, new ServiceFabricHealthReporter(healthEntityName));
        }

        internal static IConfigurationRoot ApplyFabricConfigurationOverrides(
            this IConfigurationRoot configurationRoot, 
            string configPackagePath,
            IHealthReporter healthReporter)
        {
            Debug.Assert(configurationRoot != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(configPackagePath));
            Debug.Assert(healthReporter != null);

            Regex fabricValueReferenceRegex = new Regex(FabricConfigurationValueReference, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            Regex fabricFileReferenceRegex = new Regex(FabricConfigurationFileReference, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

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

                    Match fileReferenceMatch = fabricFileReferenceRegex.Match(kvp.Value);
                    if (fileReferenceMatch.Success)
                    {
                        string configFileName = fileReferenceMatch.Groups["filename"].Value;
                        if (string.IsNullOrWhiteSpace(configFileName))
                        {
                            healthReporter.ReportWarning(
                                $"Configuration file reference '{kvp.Value}' was encountered but the file name part is missing",
                                EventFlowContextIdentifiers.Configuration);
                        }
                        else
                        {
                            string configFilePath = Path.Combine(configPackagePath, configFileName);
                            configurationRoot[kvp.Key] = configFilePath;
                        }
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    healthReporter.ReportWarning(
                                $"Configuration entry with key '{kvp.Key}' and value '{kvp.Value}' could not be checked if it represents a configuration value reference--a timeout occurred when the value was being parsed.",
                                EventFlowContextIdentifiers.Configuration);
                    continue;
                }
            }

            return configurationRoot;
        }
    }
}
