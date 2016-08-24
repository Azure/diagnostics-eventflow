// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Inputs
{
    public class TraceInputFactory : IPipelineItemFactory<TraceInput>
    {
        public TraceInput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Validation.Requires.NotNull(configuration, nameof(configuration));
            Validation.Requires.NotNull(healthReporter, nameof(healthReporter));

            IConfigurationSection traceSection = configuration
                .GetSection("inputs")
                .GetChildren()
                .FirstOrDefault(s => s["type"] == "Trace");
            return new TraceInput(traceSection, healthReporter);
        }
    }
}
