// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventListeners.Fabric
{
    using System;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;

    public class FabricSectionConfigurationProvider : IConfigurationProvider
    {
        private KeyedCollection<string, ConfigurationProperty> configurationProperties;

        public FabricSectionConfigurationProvider(string configurationSectionName)
        {
            if (string.IsNullOrWhiteSpace(configurationSectionName))
            {
                throw new ArgumentNullException("configurationSectionName");
            }

            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            ConfigurationPackage configPackage = activationContext.GetConfigurationPackageObject("Config");
            this.UseConfiguration(configPackage, configurationSectionName);
        }

        public bool HasConfiguration
        {
            get { return this.configurationProperties != null; }
        }

        public string GetValue(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            KeyedCollection<string, ConfigurationProperty> cachedConfigurationProperties = this.configurationProperties;
            if (cachedConfigurationProperties == null || !cachedConfigurationProperties.Contains(name))
            {
                return null;
            }
            else
            {
                return cachedConfigurationProperties[name].Value;
            }
        }

        public T GetValue<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return default(T);
            }

            KeyedCollection<string, ConfigurationProperty> cachedConfigurationProperties = this.configurationProperties;
            if (cachedConfigurationProperties == null || !cachedConfigurationProperties.Contains(name))
            {
                return default(T);
            }
            else
            {
                string valueString = cachedConfigurationProperties[name].Value;
                Type typeParameterType = typeof(T);
                return (T) Convert.ChangeType(valueString, typeParameterType); // Best effort
            }
        }

        private void UseConfiguration(ConfigurationPackage configPackage, string configurationSectionName)
        {
            if (!configPackage.Settings.Sections.Contains(configurationSectionName))
            {
                this.configurationProperties = null;
            }
            else
            {
                this.configurationProperties = configPackage.Settings.Sections[configurationSectionName].Parameters;
            }
        }
    }
}