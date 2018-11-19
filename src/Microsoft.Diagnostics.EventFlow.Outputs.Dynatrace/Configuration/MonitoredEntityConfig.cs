using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model
{

    public class MonitoredEntityConfig: MonitoredEntity
    {
        public string resolveFromTag { get; set; }
        public string entityAlias { get; set; }
        public TimeseriesConfig TimeSeries { get; set; }
    
        public MonitoredEntityConfig()
        { }
        public MonitoredEntityConfig(MonitoredEntityConfig entity)
        {
            resolveFromTag = entity.resolveFromTag;
            entityAlias = entity.entityAlias;
            TimeSeries = new TimeseriesConfig(entity.TimeSeries);

            displayName = entity.displayName;
            if (entity.ipAddresses != null)
                ipAddresses = entity.ipAddresses.Clone() as string[];
            if (entity.listenPorts != null)
                listenPorts = entity.listenPorts.Clone() as string[];
            type = entity.type;
            favicon = entity.favicon;
            configUrl = entity.configUrl;
            if (entity.tags != null)
                tags = entity.tags.Clone() as string[];
            if (entity.properties != null)
                properties = new Dictionary<string, string>(entity.properties);

        }
    }
}
