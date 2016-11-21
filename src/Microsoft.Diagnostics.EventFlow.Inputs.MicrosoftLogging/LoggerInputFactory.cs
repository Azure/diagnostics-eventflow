// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class LoggerInputFactory : IPipelineItemFactory<LoggerInput>
    {
        public LoggerInput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Validation.Requires.NotNull(healthReporter, nameof(healthReporter));

            return new LoggerInput(healthReporter);
        }
    }
}
