// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Fabric;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Diagnostics.EventListeners.Fabric
{
    public class FabricJsonFileConfigurationProvider : JsonObjectConfigurationProvider
    {
        public FabricJsonFileConfigurationProvider(string configurationFileName): base(null)
        {
            if (string.IsNullOrWhiteSpace(configurationFileName))
            {
                throw new ArgumentNullException(nameof(configurationFileName));
            }

            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            ConfigurationPackage configPackage = activationContext.GetConfigurationPackageObject("Config");
            string configFilePath = Path.Combine(configPackage.Path, configurationFileName);
            if (!File.Exists(configFilePath))
            {
                return;
            }

            using (StreamReader sr = new StreamReader(configFilePath))
            {
                this.configuration = (JObject) JToken.ReadFrom(new JsonTextReader(sr));
            }            
        }
    }
}
