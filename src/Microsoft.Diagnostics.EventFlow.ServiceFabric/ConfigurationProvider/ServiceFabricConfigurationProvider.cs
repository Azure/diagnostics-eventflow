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
            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            activationContext.ConfigurationPackageModifiedEvent += (sender, e) => {
                this.Load(e.NewPackage, reload: true);
                this.OnReload();
            };
        }

        public override void Load()
        {
            ConfigurationPackage configPackage = FabricRuntime.GetActivationContext().GetConfigurationPackageObject(this.configurationPackageName);
            this.Load(configPackage);
        }

        private void Load(ConfigurationPackage configPackage, bool reload = false)
        {
            if (reload)
            {
                Data.Clear();
            }

            foreach (var configurationSection in configPackage.Settings.Sections)
            {
                foreach (var property in configurationSection.Parameters)
                {
                    string propertyPath = ConfigurationPath.Combine(configurationSection.Name, property.Name);
                    Data[propertyPath] = property.IsEncrypted ? property.DecryptValue().ToUnsecureString() : property.Value;
                }
            }
        }
    }
}
