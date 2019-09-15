using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    public class ElasticSearchMappingsConfigurationPropertyDescriptor
    {
        public string Type { get; set; }

        internal ElasticSearchMappingsConfigurationPropertyDescriptor DeepClone()
        {
            var other = new ElasticSearchMappingsConfigurationPropertyDescriptor
            {
                Type = this.Type
            };

            return other;
        }
    }
}
