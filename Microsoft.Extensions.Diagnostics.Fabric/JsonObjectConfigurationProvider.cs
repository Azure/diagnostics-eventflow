// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Fabric;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Microsoft.Extensions.Diagnostics.Fabric
{
    public class JsonObjectConfigurationProvider: ICompositeConfigurationProvider
    {
        protected JObject configuration;

        public JsonObjectConfigurationProvider(JObject configuration)
        {
            this.configuration = configuration;
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

        public ICompositeConfigurationProvider GetConfiguration(string configurationName)
        {
            if (string.IsNullOrEmpty(configurationName))
            {
                return null;
            }

            JObject child = this.configuration?.GetValue(configurationName) as JObject;
            return child == null ? null : new JsonObjectConfigurationProvider(child);
        }
    }
}
