//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal class PositiveEvaluator : FilterEvaluator
    {
        public static Lazy<PositiveEvaluator> Instance = new Lazy<PositiveEvaluator>();

        public override string SemanticsString
        {
            get { return "(__PositiveEvaluator)"; }
        }

        public override bool Evaluate(EventData e)
        {
            return true;
        }
    }
}
