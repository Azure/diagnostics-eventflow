// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    public class ElasticSearchMappingsConfiguration
    {
        public Dictionary<string, ElasticSearchMappingsConfigurationPropertyDescriptor> Properties { get; set; }

        public ElasticSearchMappingsConfiguration()
        {
            Properties = new Dictionary<string, ElasticSearchMappingsConfigurationPropertyDescriptor>();
        }

        internal ElasticSearchMappingsConfiguration DeepClone()
        {
            var other = new ElasticSearchMappingsConfiguration();

            foreach (var item in this.Properties)
            {
                other.Properties.Add(item.Key, item.Value.DeepClone());
            }

            return other;
        }
    }
}
