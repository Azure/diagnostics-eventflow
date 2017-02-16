// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration.ServiceFabric;

namespace Microsoft.Extensions.Configuration
{
    public static class ServiceFabricExtensions
    {
        public static IConfigurationBuilder AddServiceFabric(this IConfigurationBuilder builder)
            => builder.Add(new ServiceFabricConfigurationSource());

        public static IConfigurationBuilder AddServiceFabric(this IConfigurationBuilder builder, string configurationPackageName)
            => builder.Add(new ServiceFabricConfigurationSource { ConfigurationPackageName = configurationPackageName });

        public static IConfigurationBuilder AddServiceFabric(this IConfigurationBuilder builder, Action<ServiceFabricConfigurationSource> configureSource)
        {
            var source = new ServiceFabricConfigurationSource();
            configureSource(source);
            return builder.Add(source);
        }
    }
}
