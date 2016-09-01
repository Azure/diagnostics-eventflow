// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Diagnostics.EventFlow.FilterEvaluators;

namespace Microsoft.Diagnostics.EventFlow.Filters
{
    public class DropFilter : IncludeConditionFilter
    {
        public DropFilter(string includeCondition = null) : base(includeCondition)
        {
        }

        public override FilterResult Evaluate(EventData eventData)
        {
            return this.Evaluator.Evaluate(eventData) ? FilterResult.DiscardEvent : FilterResult.KeepEvent;
        }
    }
}
