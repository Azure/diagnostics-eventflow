// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Diagnostics.EventListeners.Fabric
{
    public class FabricJsonFileConfigurationProvider : IConfigurationProvider
    {
        private JObject configuration;

        public FabricJsonFileConfigurationProvider(string configurationFileName)
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

        public bool HasConfiguration
        {
            get
            {
                return this.configuration != null;
            }
        }

        public string GetValue(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return this.configuration?.GetValue(name)?.ToString();            
        }

        public T GetValue<T>(string name)
        {
            string valueString = this.GetValue(name);
            if (string.IsNullOrWhiteSpace(valueString))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(valueString);
        }
    }
}
