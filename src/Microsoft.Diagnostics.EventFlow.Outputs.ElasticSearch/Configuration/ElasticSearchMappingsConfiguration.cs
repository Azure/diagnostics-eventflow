using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    public class ElasticSearchMappingsConfiguration
    {
        public Dictionary<string, ElasticSearchMappingsConfigurationPropertyDescriptor> Properties { get; set; }

        internal ElasticSearchMappingsConfiguration DeepClone()
        {
            var other = new ElasticSearchMappingsConfiguration();
            other.Properties = new Dictionary<string, ElasticSearchMappingsConfigurationPropertyDescriptor>();

            foreach (var item in this.Properties)
            {
                other.Properties.Add(item.Key, item.Value.DeepClone());
            }

            return other;
        }
    }
}
