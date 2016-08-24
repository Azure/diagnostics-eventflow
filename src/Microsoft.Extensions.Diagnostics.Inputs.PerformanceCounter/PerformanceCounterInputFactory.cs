// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Extensions.Diagnostics.Inputs
{
    public class PerformanceCounterListenerFactory: IPipelineItemFactory<PerformanceCounterInput>
    {
        public PerformanceCounterInput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));

            // TODO: implement
            throw new NotImplementedException();
        }
    }
}
