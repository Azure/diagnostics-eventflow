// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    /// <summary>
    /// Factory for NLog input elements.
    /// </summary>
    public class NLogInputFactory : IPipelineItemFactory<NLogInput>
    {
        /// <inheritdoc/>
        public NLogInput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            return new NLogInput(healthReporter);
        }
    }
}
