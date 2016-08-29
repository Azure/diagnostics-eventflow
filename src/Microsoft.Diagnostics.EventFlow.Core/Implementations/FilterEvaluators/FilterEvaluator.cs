//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal abstract class FilterEvaluator
    {
        public abstract string SemanticsString { get; }

        public abstract bool Evaluate(EventData e);
    }
}
