//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Extensions.Diagnostics.FilterEvaluators
{
    /// <summary>
    /// A base class for operators that involve ordering, i.e. <, >, >= and <=
    /// </summary>
    internal abstract class OrderingEvaluator : CachingPropertyExpressionEvaluator
    {
        public OrderingEvaluator(string propertyName, string value) : base(propertyName, value) { }

        /// <summary>
        /// Evaluates the comparison result approprietly for the given provider (implemented in the derived class).
        /// The comparison result follows .NET conventions (string.Compare(), IComparable.CompareTo(), where
        /// a value less than zero means left-hand side is 'smaller' than right-hand side, zero means equality,
        /// and a value greater than zero means LHS is 'larger' than RHS
        /// </summary>
        protected abstract bool IsMatch(int comparisonResult);

        public override bool Evaluate(EventData e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            object eventPropertyValue;
            if (!e.TryGetPropertyValue(this.propertyName, out eventPropertyValue))
            {
                return false;
            }

            var interpretedValue = GetOrAddInterpretedRHSValue(eventPropertyValue);

            bool retval;

            if (interpretedValue == null)
            {
                retval = false;
            }
            else if (eventPropertyValue is string)
            {
                retval = IsMatch(string.Compare((string)eventPropertyValue, (string)interpretedValue, StringComparison.OrdinalIgnoreCase));
            }
            else if (eventPropertyValue is bool || eventPropertyValue is Guid)
            {
                // bool and Guid do implement IComparable, but we do not want to use that implementation here, because we expect user only
                // use equality/inequality operator but not ordering operator for such types. The evaluation for these types always return false.
                retval = false;
            }
            else if (eventPropertyValue is IComparable)
            {
                retval = IsMatch(((IComparable)eventPropertyValue).CompareTo(interpretedValue));
            }
            else
            {
                // Do not know how to compare properties of the current type
                return false;
            }

            return retval;
        }
    }
}
