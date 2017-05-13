// ------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    public abstract class FilterEvaluator
    {
        public abstract string SemanticsString { get; }

        public abstract bool Evaluate(EventData e);
    }
}
