// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Fabric;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Extensions.Configuration.ServiceFabric
{
    public class ServiceFabricConfigurationProvider: ConfigurationProvider
    {
        public const string DefaultConfigurationPackageName = "Config";

        private string configurationPackageName;

        public ServiceFabricConfigurationProvider(string configurationPackageName = DefaultConfigurationPackageName)
        {
            Requires.NotNullOrEmpty(configurationPackageName, nameof(configurationPackageName));

            this.configurationPackageName = configurationPackageName;
        }

        public override void Load()
        {
            // TODO: react to configuration changes by listening to ConfiugrationPackageActivationContext.ConfigurationPackageModifiedEvent

            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            ConfigurationPackage configPackage = activationContext.GetConfigurationPackageObject(this.configurationPackageName);
            foreach (var configurationSection in configPackage.Settings.Sections)
            {
                foreach(var property in configurationSection.Parameters)
                {
                    // We omit encrypted values due to security concerns--if you need them, use Service Fabric APIs to access them
                    if (!property.IsEncrypted)
                    {
                        Data[ConfigurationPath.Combine(configurationSection.Name, property.Name)] = property.Value;
                    }
                }
            }
        }
    }
}
