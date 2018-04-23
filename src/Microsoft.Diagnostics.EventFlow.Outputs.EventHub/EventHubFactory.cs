// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Diagnostics.EventFlow.Metadata;
using Microsoft.Diagnostics.EventFlow.Outputs.EventHub;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    public class EventHubOutputFactory : IPipelineItemFactory<IOutput>
    {
        public IOutput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            //Check wether the optional partitionKeyProperty configuration element is set. If so, return an instance of PartitionedEventHubOutput
            string partitioKeyProperty = configuration[PartitionKeyData.PartitionKeyPropertyMoniker];
            if (string.IsNullOrWhiteSpace(partitioKeyProperty) == false)
            {
                return new PartitionedEventHubOutput(configuration, healthReporter);
            }
            else //return output handler that does not use Event Hub partinions
            {
                return new EventHubOutput(configuration, healthReporter);
            }
        }
    }
}