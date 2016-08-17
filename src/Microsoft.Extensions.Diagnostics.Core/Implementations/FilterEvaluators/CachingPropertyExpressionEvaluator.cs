//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Extensions.Diagnostics.FilterEvaluators
{
    internal abstract class CachingPropertyExpressionEvaluator : EventPropertyExpressionEvaluator
    {
        // Different ETW events may have properties with the same names but different types, so we need to try to interpret the RHS value
        // according to property type of a particular event we are evaluating.
        // We use the dictionary below to cache interpreted values.
        // If the entry (key) exists, but the cached value is null, it means we tried, but failed to interpret the RHS value as a value of given type.
        private Lazy<IDictionary<Type, object>> interpretedValueCache = new Lazy<IDictionary<Type, object>>(() => new Dictionary<Type, object>());
        private Type lastInterpretedValueType;  // The type used to interpret the expression right-hand side value

        public CachingPropertyExpressionEvaluator(string propertyName, string value) : base(propertyName, value)
        {
        }

        protected object LastInterpretedValue { get; private set; }    // Most recently used, interpreted value as specified in the right-hand side of the expression

        protected void EnsureLastInterpretedRhsValue(object eventPropertyValue)
        {
            if (eventPropertyValue == null)
            {
                throw new ArgumentNullException(nameof(eventPropertyValue));
            }

            Type eventPropertyValueType = eventPropertyValue.GetType();
            if (eventPropertyValueType == this.lastInterpretedValueType)
            {
                return;
            }

            IDictionary<Type, object> valueCache = this.interpretedValueCache.Value;

            // Make sure we do not lose the currently interpreted value
            if (this.lastInterpretedValueType != null)
            {
                valueCache[this.lastInterpretedValueType] = this.LastInterpretedValue;
            }

            object newInterpretedValue = null;
            if (!valueCache.TryGetValue(eventPropertyValueType, out newInterpretedValue))
            {
                newInterpretedValue = InterpretRhsValue(eventPropertyValueType);
            }

            this.lastInterpretedValueType = eventPropertyValueType;
            this.LastInterpretedValue = newInterpretedValue;
        }

        private object InterpretRhsValue(Type eventPropertyValueType)
        {
            Debug.Assert(eventPropertyValueType != null);

            // string is the most commonly used type so we will check it first for performance reasons
            // The way Convert.ChangeType() works does not meet our expectations with regards to Guid, DateTime and DateTimeOffset,
            // so we will special-case these types too.
            if (eventPropertyValueType == typeof(string))
            {
                return this.value;
            }
            else if (eventPropertyValueType == typeof(Guid))
            {
                Guid parsedGuidValue;
                if (Guid.TryParse(this.value, out parsedGuidValue))
                {
                    return parsedGuidValue;
                }
                else return null;
            }
            else if (eventPropertyValueType == typeof(DateTime))
            {
                DateTime parsedDateTimeValue;
                if (DateTime.TryParse(this.value, out parsedDateTimeValue))
                {
                    return parsedDateTimeValue;
                }
                else return null;
            }
            else if (eventPropertyValueType == typeof(DateTimeOffset))
            {
                DateTimeOffset parsedDateTimeOffsetValue;
                if (DateTimeOffset.TryParse(this.value, out parsedDateTimeOffsetValue))
                {
                    return parsedDateTimeOffsetValue;
                }
                else return null;
            }
            else
            {
                object retval = null;
                try
                {
                    retval = Convert.ChangeType(this.value, eventPropertyValueType, CultureInfo.CurrentCulture);
                }
                catch { }
                return retval;
            }
        }
    }
}
