// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Filters;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Filters
{
    public class DropFilterFactory: IPipelineItemFactory<DropFilter>
    {
        public DropFilter CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            var filterConfiguration = new IncludeConditionFilterConfiguration();
            try
            {
                configuration.Bind(filterConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"{nameof(DropFilterFactory)}: configuration is invalid for filter {configuration.ToString()}");
                return null;
            }

            return new DropFilter(filterConfiguration.Include);
        }
    }
}
