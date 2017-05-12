//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal class BitwiseEqualityEvaluator : EventPropertyExpressionEvaluator
    {
        // Convert the rhs value to both unsigned and signed format.
        // The reason is complier will complain that operator '&' can't be applied with operands of type 'ulong' and signed value.
        // While we do want to handle the case when the lhs value is a negative number, in this case, the signed rhs value will be used.
        private ulong unsignedRhsValue;
        private long signedRhsValue;

        public BitwiseEqualityEvaluator(string propertyName, string value) : base(propertyName, value)
        {
            bool isValidRhsValue = false;
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                isValidRhsValue = ulong.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out this.unsignedRhsValue)
                    && long.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out this.signedRhsValue);
            }
            else
            {
                isValidRhsValue = ulong.TryParse(value, out this.unsignedRhsValue)
                    && long.TryParse(value, out this.signedRhsValue);
            }

            if (!isValidRhsValue)
            {
                throw new ArgumentException($"The rhs value '{value}' of &= operator is not integral type");
            }
        }

        public override string SemanticsString
        {
            get { return string.Format(CultureInfo.InvariantCulture, "(__BitwiseEqualityEvaluator:{0}&=={1})", this.propertyName, this.value); }
        }

        public override bool Evaluate(EventData e)
        {
            object propertyValue;
            if (!e.TryGetPropertyValue(this.propertyName, out propertyValue))
            {
                return false;
            }

            var valueAsString = propertyValue.ToString();
            if (valueAsString.StartsWith("-"))
            {
                long lhsValue;
                if (long.TryParse(valueAsString, out lhsValue))
                {
                    return (lhsValue & this.signedRhsValue) == this.signedRhsValue;
                }
            }
            else
            {
                ulong lhsValue;
                if(ulong.TryParse(valueAsString, out lhsValue))
                {
                    return (lhsValue & this.unsignedRhsValue) == this.unsignedRhsValue;
                }
            }

            return false;
        }
    }
}
