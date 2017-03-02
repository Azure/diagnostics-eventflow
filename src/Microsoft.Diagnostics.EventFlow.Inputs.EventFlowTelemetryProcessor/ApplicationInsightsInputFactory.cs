// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class ApplicationInsightsInputFactory : IPipelineItemFactory<EventFlowSubject<EventData>>
    {
        public static readonly string ApplicationInsightsInputTag = "ApplicationInsightsInput";

        public EventFlowSubject<EventData> CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            var input = new EventFlowSubject<EventData>();
            input.Labels.Add(ApplicationInsightsInputFactory.ApplicationInsightsInputTag, null);
            return input;
        }
    }
}
