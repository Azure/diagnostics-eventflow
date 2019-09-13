using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Nest;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    internal class CreateIndexRequestBuilder
    {
        private readonly ElasticSearchOutputConfiguration configuration;
        private readonly IHealthReporter healthReporter;

        internal CreateIndexRequestBuilder(Configuration.ElasticSearchOutputConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.configuration = configuration;
            this.healthReporter = healthReporter;
        }

        static readonly Dictionary<string, Func<PropertiesDescriptor<object>, string, PropertiesDescriptor<object>>> typeToPropertiesDesctiptorFunc =
            new Dictionary<string, Func<PropertiesDescriptor<object>, string, PropertiesDescriptor<object>>>
            {
                ["text"] = (pd, name) => pd.Text(a => a.Name(name)),
                ["keyword"] = (pd, name) => pd.Keyword(p => p.Name(name)),
                ["date"] = (pd, name) => pd.Date(p => p.Name(name)),
                ["date_nanos"] = (pd, name) => pd.DateNanos(p => p.Name(name)),
                ["boolean"] = (pd, name) => pd.Boolean(p => p.Name(name)),
                ["long"] = (pd, name) => pd.Number(p => p.Name(name).Type(NumberType.Long)),
                ["integer"] = (pd, name) => pd.Number(p => p.Name(name).Type(NumberType.Integer)),
                ["short"] = (pd, name) => pd.Number(p => p.Name(name).Type(NumberType.Short)),
                ["byte"] = (pd, name) => pd.Number(p => p.Name(name).Type(NumberType.Byte)),
                ["double"] = (pd, name) => pd.Number(p => p.Name(name).Type(NumberType.Double)),
                ["float"] = (pd, name) => pd.Number(p => p.Name(name).Type(NumberType.Float)),
                ["half_float"] = (pd, name) => pd.Number(p => p.Name(name).Type(NumberType.HalfFloat)),
                ["scaled_float"] = (pd, name) => pd.Number(p => p.Name(name).Type(NumberType.ScaledFloat)),
                ["ip"] = (pd, name) => pd.Ip(p => p.Name(name)),
                ["geo_point"] = (pd, name) => pd.GeoPoint(p => p.Name(name)),
                ["geo_shape"] = (pd, name) => pd.GeoShape(p => p.Name(name)),
                ["completion"] = (pd, name) => pd.Completion(p => p.Name(name))
            };


        private TypeMappingDescriptor<object> mappingsSelector(TypeMappingDescriptor<object> tm)
        {
            return tm.Properties(pd =>
            {
                PropertiesDescriptor<object> properties = pd;
                foreach (var propMapping in configuration.Mappings.Properties)
                {
                    string propertyType = propMapping.Value.Type;
                    string propertyName = propMapping.Key;

                    if (!typeToPropertiesDesctiptorFunc.ContainsKey(propertyType))
                    {
                        string errorMessage = $"{nameof(ElasticSearchOutput)}: {propertyName} property mapping could not be set because configured type ({propertyType}) is not supported.";
                        healthReporter.ReportWarning(errorMessage, EventFlowContextIdentifiers.Output);
                    }
                    else
                    {
                        properties = typeToPropertiesDesctiptorFunc[propertyType](properties, propertyName);
                    }
                }

                return properties;
            });
        }

        private IndexSettingsDescriptor settingsSelector(IndexSettingsDescriptor s)
        {
            s = s.NumberOfReplicas(configuration.NumberOfReplicas)
                .NumberOfShards(configuration.NumberOfShards)
                .RefreshInterval(configuration.RefreshInterval);

            if (!string.IsNullOrWhiteSpace(configuration.DefaultPipeline))
                s = s.DefaultPipeline(configuration.DefaultPipeline);

            return s;
        }

        internal ICreateIndexRequest Selector(CreateIndexDescriptor c)
        {
            return 
                c.Settings(settingsSelector)
                .Map(mappingsSelector);
        }
    }
}
