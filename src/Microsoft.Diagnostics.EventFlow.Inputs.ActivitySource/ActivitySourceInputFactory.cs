// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Inputs.ActivitySource
{
    public class ActivitySourceInputFactory : IPipelineItemFactory<ActivitySourceInput>
    {
        public ActivitySourceInput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            return new ActivitySourceInput(configuration, healthReporter);
        }
    }
}
