// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    public class EventHubOutputFactory : IPipelineItemFactory<EventHubOutput>
    {
        public EventHubOutput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            return new EventHubOutput(configuration, healthReporter);
        }
    }
}
