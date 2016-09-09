// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class DummyFilter : IFilter
    {
        public FilterResult Evaluate(EventData eventData)
        {
            return FilterResult.KeepEvent;
        }
    }

    public class DummyFilterFactory : IPipelineItemFactory<DummyFilter>
    {
        public DummyFilter CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            return new DummyFilter();
        }
    }
}
