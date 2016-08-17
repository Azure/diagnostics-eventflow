//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Extensions.Diagnostics.FilterEvaluators
{
    internal class AlternativeEvaluator : FilterEvaluator
    {
        private readonly FilterEvaluator first;
        private readonly FilterEvaluator second;

        public AlternativeEvaluator(FilterEvaluator first, FilterEvaluator second)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            this.first = first;
            this.second = second;
        }

        public override bool Evaluate(EventData e)
        {
            return this.first.Evaluate(e) || this.second.Evaluate(e);
        }

        public override string SemanticsString
        {
            get { return string.Format(CultureInfo.InvariantCulture, "({0}OR{1})", this.first.SemanticsString, this.second.SemanticsString); }
        }
    }
}
