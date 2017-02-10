//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal abstract class CachingPropertyExpressionEvaluator : EventPropertyExpressionEvaluator
    {
        // Different events may have properties with the same names but different types, so we need to try to interpret the RHS value
        // according to property type of a particular event we are evaluating. If the entry (key) exists, but the cached value is null,
        // it means we tried, but failed to interpret the RHS value as a value of given type.
        //
        // The cache will be accessed by multiple threads. If we use a general dictionary as the cache, thread safety is not ensured.
        // Given the fact that there are much more read operations than write, using a lock on read operation is not efficient.
        // Thus we use the ImmutableDictionary as the cache. It's guaranteed a cache instance is immutable, so there is no need to lock when read the cache.
        private IImmutableDictionary<Type, object> interpretedValueCache = ImmutableDictionary<Type, object>.Empty;
        private object lockObj = new object();

        public CachingPropertyExpressionEvaluator(string propertyName, string value) : base(propertyName, value)
        {
        }

        protected object GetOrAddInterpretedRHSValue(object eventPropertyValue)
        {
            object value;
            Type lhsValueType = eventPropertyValue.GetType();

            if (!interpretedValueCache.TryGetValue(lhsValueType, out value))
            {
                lock(lockObj)
                {
                    if (!interpretedValueCache.TryGetValue(lhsValueType, out value))
                    {
                        var interpretedValue = InterpretRhsValue(lhsValueType);
                        interpretedValueCache = interpretedValueCache.Add(lhsValueType, interpretedValue);

                        return interpretedValue;
                    }
                }
            }

            return value;
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
            else if (eventPropertyValueType.GetTypeInfo().IsEnum)
            {
                object retval = null;
                try
                {
                    retval = Enum.Parse(eventPropertyValueType, this.value);
                }
                catch (Exception) { }
                return retval;
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
