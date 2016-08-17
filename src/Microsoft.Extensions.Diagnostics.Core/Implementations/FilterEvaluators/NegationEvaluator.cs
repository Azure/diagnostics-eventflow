//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Extensions.Diagnostics.FilterEvaluators
{
    internal class NegationEvaluator : FilterEvaluator
    {
        private readonly FilterEvaluator inner;

        public NegationEvaluator(FilterEvaluator inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            this.inner = inner;
        }

        public override bool Evaluate(EventData e)
        {
            return !this.inner.Evaluate(e);
        }

        public override string SemanticsString
        {
            get { return string.Format(CultureInfo.InvariantCulture, "(NOT{0})", this.inner.SemanticsString); }
        }
    }
}
