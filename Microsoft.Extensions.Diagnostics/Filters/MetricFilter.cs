// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.Metadata;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public class MetricFilter: IEventFilter<EventData>
    {
        private IMetricMetadataCollection metricMetadataCollection;

        public MetricFilter(IMetricMetadataCollection metricMetadataCollection, IHealthReporter healthReporter)
        {
            Requires.NotNull(metricMetadataCollection, nameof(metricMetadataCollection));

            this.metricMetadataCollection = metricMetadataCollection;
        }

        public bool Filter(EventData eventData)
        {
            MetricMetadata metricMetadata = this.metricMetadataCollection.GetMetadata(eventData.ProviderName, eventData.EventName);
            if (metricMetadata != null)
            {
                eventData.SetMetadata(typeof(MetricMetadata), metricMetadata);
            }

            return true;
        }
    }
}
