using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    public class ElasticSearchMappingsConfiguration
    {
        public Dictionary<string, ElasticSearchMappingsConfigurationPropertyDescriptor> Properties { get; private set; }

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
