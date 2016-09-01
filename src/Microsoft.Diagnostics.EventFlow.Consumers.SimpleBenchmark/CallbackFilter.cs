// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Diagnostics.EventFlow;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Consumers.SimpleBenchmark
{
    internal class CallbackFilter : IFilter
    {
        private Func<EventData, bool> evaluate;

        public CallbackFilter(Func<EventData, bool> evaluate)
        {
            Requires.NotNull(evaluate, nameof(evaluate));
            this.evaluate = evaluate;
        }

        public FilterResult Evaluate(EventData eventData)
        {
            return this.evaluate(eventData) ? FilterResult.KeepEvent : FilterResult.DiscardEvent;
        }
    }
}
