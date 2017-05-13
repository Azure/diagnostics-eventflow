// ------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal class InequalityEvaluator : EqualityEvaluator
    {
        public InequalityEvaluator(string propertyName, string value) : base(propertyName, value) { }

        public override string SemanticsString
        {
            get { return string.Format(CultureInfo.InvariantCulture, "(__InequalityEvaluator:{0}!={1})", this.propertyName, this.value); }
        }

        public override bool Evaluate(EventData e)
        {
            bool fullyEvaluated;
            bool equal = EvaluateEquality(e, out fullyEvaluated);
            return fullyEvaluated && !equal;
        }
    }
}
