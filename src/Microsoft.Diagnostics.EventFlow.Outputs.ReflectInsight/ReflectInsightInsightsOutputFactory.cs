// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.Diagnostics.EventFlow.IPipelineItemFactory{Microsoft.Diagnostics.EventFlow.Outputs.ReflectInsightOutput}" />
    public class ReflectInsightOutputFactory : IPipelineItemFactory<ReflectInsightOutput>
    {
        /// <summary>
        /// Creates the item.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="healthReporter">The health reporter.</param>
        /// <returns></returns>
        public ReflectInsightOutput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            return new ReflectInsightOutput(configuration, healthReporter);
        }
    }
}
