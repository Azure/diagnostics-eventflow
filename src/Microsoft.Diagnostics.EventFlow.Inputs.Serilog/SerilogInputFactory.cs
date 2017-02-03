// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    /// <summary>
    /// Factory for Serilog input elements.
    /// </summary>
    public class SerilogInputFactory : IPipelineItemFactory<SerilogInput>
    {
        /// <inheritdoc/>
        public SerilogInput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            return new SerilogInput(healthReporter);
        }
    }
}
