// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Configuration.ServiceFabric
{
    public class ServiceFabricConfigurationSource : IConfigurationSource
    {
        public string ConfigurationPackageName { get; set; }

        public ServiceFabricConfigurationSource()
        {
            this.ConfigurationPackageName = ServiceFabricConfigurationProvider.DefaultConfigurationPackageName;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ServiceFabricConfigurationProvider(this.ConfigurationPackageName);
        }
    }
}
