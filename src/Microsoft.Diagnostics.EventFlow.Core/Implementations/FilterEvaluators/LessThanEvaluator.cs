﻿// ------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal class LessThanEvaluator : OrderingEvaluator
    {
        public LessThanEvaluator(string propertyName, string value) : base(propertyName, value) { }

        public override string SemanticsString
        {
            get { return string.Format(CultureInfo.InvariantCulture, "(__LessThanEvaluator:{0}<{1})", this.propertyName, this.value); }
        }

        protected override bool IsMatch(int comparisonResult)
        {
            return comparisonResult < 0;
        }
    }
}
