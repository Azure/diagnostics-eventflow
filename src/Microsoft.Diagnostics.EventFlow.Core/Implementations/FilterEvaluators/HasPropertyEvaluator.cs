// ------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal class HasPropertyEvaluator : FilterEvaluator
    {
        private readonly string propertyName;

        public HasPropertyEvaluator(string propertyName)
        {
            this.propertyName = propertyName;
        }

        public override string SemanticsString
        {
            get { return string.Format(CultureInfo.InvariantCulture, "(hasproperty {0})", this.propertyName); }
        }

        public override bool Evaluate(EventData e)
        {
            return e.TryGetPropertyValue(this.propertyName, out _);
        }
    }
}