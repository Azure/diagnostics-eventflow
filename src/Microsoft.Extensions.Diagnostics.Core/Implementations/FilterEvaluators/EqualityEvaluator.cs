//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Extensions.Diagnostics.FilterEvaluators
{
    internal class EqualityEvaluator : CachingPropertyExpressionEvaluator
    {
        public EqualityEvaluator(string propertyName, string value) : base(propertyName, value) { }

        public override string SemanticsString
        {
            get { return string.Format(CultureInfo.InvariantCulture, "(__EqualityEvaluator:{0}=={1})", this.propertyName, this.value); }
        }

        public override bool Evaluate(EventData e)
        {
            bool ignored;
            return EvaluateEquality(e, out ignored);
        }

        protected bool EvaluateEquality(EventData e, out bool fullyEvaluated)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }
            fullyEvaluated = true;

            object eventPropertyValue;
            if (!e.TryGetPropertyValue(this.propertyName, out eventPropertyValue))
            {
                fullyEvaluated = false;
                return false;
            }

            var interpretedValue = GetOrAddInterpretedRHSValue(eventPropertyValue);
            if (interpretedValue == null)
            {
                fullyEvaluated = false;
                return false;
            }

            if (eventPropertyValue is string)
            {
                return string.Equals((string)eventPropertyValue, (string)interpretedValue, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return eventPropertyValue.Equals(interpretedValue);
            }
        }
    }
}
