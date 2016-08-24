// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Extensions.Diagnostics.Outputs
{
    public class ApplicationInsightsOutputFactory: IPipelineItemFactory<ApplicationInsightsOutput>
    {
        public ApplicationInsightsOutput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            return new ApplicationInsightsOutput(configuration, healthReporter);
        }
    }
}
