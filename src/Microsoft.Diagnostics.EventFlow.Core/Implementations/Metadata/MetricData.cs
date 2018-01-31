// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Validation;

namespace Microsoft.Diagnostics.EventFlow.Metadata
{
    public class MetricData
    {
        public static readonly string MetricMetadataKind = "metric";
        public static readonly string MetricNameMoniker = "metricName";
        public static readonly string MetricNamePropertyMoniker = "metricNameProperty";
        public static readonly string MetricValueMoniker = "metricValue";
        public static readonly string MetricValuePropertyMoniker = "metricValueProperty";

        public string MetricName { get; private set; }
        public double Value { get; private set; }

        // Ensure that MetricData can only be created using TryGetMetricData() method
        private MetricData() { }

        public static DataRetrievalResult TryGetData(EventData eventData, EventMetadata metricMetadata, out MetricData metric)
        {
            Requires.NotNull(eventData, nameof(eventData));
            Requires.NotNull(metricMetadata, nameof(metricMetadata));
            metric = null;

            if (!MetricMetadataKind.Equals(metricMetadata.MetadataType, System.StringComparison.OrdinalIgnoreCase))
            {
                return DataRetrievalResult.InvalidMetadataType(metricMetadata.MetadataType, MetricMetadataKind);
            }

            string metricName = string.Empty;

            string metricNameProperty = metricMetadata[MetricNamePropertyMoniker];
            if (string.IsNullOrEmpty(metricNameProperty))
            {
                metricName = metricMetadata[MetricNameMoniker];
                if (string.IsNullOrEmpty(metricName))
                {
                    return DataRetrievalResult.MissingMetadataProperty(MetricNameMoniker);
                }
            }
            else
            {
                if (!eventData.GetValueFromPayload<string>(metricNameProperty, (v) => metricName = v))
                {
                    return DataRetrievalResult.DataMissingOrInvalid(metricNameProperty);
                }
            }

            double value = 0.0;

            string metricValueProperty = metricMetadata[MetricValuePropertyMoniker];
            if (string.IsNullOrEmpty(metricValueProperty))
            {
                string rawValue = metricMetadata[MetricValueMoniker];
                if (string.IsNullOrEmpty(rawValue))
                {
                    return DataRetrievalResult.MissingMetadataProperty(MetricValueMoniker);
                }

                if (!double.TryParse(rawValue, out value))
                {
                    return DataRetrievalResult.InvalidMetadataPropertyValue(MetricValueMoniker, rawValue);
                }
            }
            else
            {
                if (!eventData.GetValueFromPayload<double>(metricValueProperty, (v) => value = v))
                {
                    return DataRetrievalResult.DataMissingOrInvalid(metricValueProperty);
                }
            }

            metric = new MetricData();
            metric.MetricName = metricName;
            metric.Value = value;

            return DataRetrievalResult.Success;
        }
    }
}
