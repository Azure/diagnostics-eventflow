// ------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal abstract class CachingPropertyExpressionEvaluator : EventPropertyExpressionEvaluator
    {
        private delegate bool NumberParser<T>(string value, NumberStyles numberStyles, IFormatProvider formatProvider, out T result);

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
            // Special-case integer properties to allow hexadecimal notation
            else if (eventPropertyValueType == typeof(long))
            {
                return ParseNumber<long>(Int64.TryParse);
            }
            else if (eventPropertyValueType == typeof(ulong))
            {
                return ParseNumber<ulong>(UInt64.TryParse);
            }
            else if (eventPropertyValueType == typeof(int))
            {
                return ParseNumber<int>(Int32.TryParse);
            }
            else if (eventPropertyValueType == typeof(uint))
            {
                return ParseNumber<uint>(UInt32.TryParse);
            }
            else if (eventPropertyValueType == typeof(short))
            {
                return ParseNumber<short>(Int16.TryParse);
            }
            else if (eventPropertyValueType == typeof(ushort))
            {
                return ParseNumber<ushort>(UInt16.TryParse);
            }
            else if (eventPropertyValueType == typeof(byte))
            {
                return ParseNumber<byte>(Byte.TryParse);
            }
            else if (eventPropertyValueType == typeof(sbyte))
            {
                return ParseNumber<sbyte>(SByte.TryParse);
            }
            else if (eventPropertyValueType.GetTypeInfo().IsEnum)
            {
                // Allow using enumeration value names instead of actual values.
                object retval = null;
                try
                {
                    retval = Enum.Parse(eventPropertyValueType, this.value);
                }
                catch { }
                return retval;
            }
            else
            {
                object retval = null;
                try
                {
                    retval = Convert.ChangeType(this.value, eventPropertyValueType, CultureInfo.InvariantCulture);
                }
                catch { }

                // On .NET Core 3.0 Convert.ChangeType() may return +/- infinity when overflow occurs. 
                // We do not consider these to be valid values to compare with.
                if (retval != null)
                {
                    if (retval.Equals(double.PositiveInfinity) || retval.Equals(double.NegativeInfinity) || retval.Equals(float.PositiveInfinity) || retval.Equals(float.NegativeInfinity))
                    {
                        retval = null;
                    }
                }
                
                return retval;
            }
        }

        private object ParseNumber<T>(NumberParser<T> parser)
        {
            if (this.value != null && this.value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                // Event if HexNumber is set, the .NET Framework TryParse() methods do not allow the '0x' prefix to be part of the value.
                if (parser(this.value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out T result))
                {
                    return result;
                }
            }
            else
            {
                if (parser(this.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out T result))
                {
                    return result;
                }
            }

            return null;
        }
    }
}
