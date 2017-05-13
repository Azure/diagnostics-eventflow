// ------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
                throw new ArgumentException($"The right-hand side value '{value}' of &== operator is not integral type");
            }
        }

        public override string SemanticsString
        {
            get { return string.Format(CultureInfo.InvariantCulture, "(__BitwiseEqualityEvaluator:{0}&=={1})", this.propertyName, this.value); }
        }

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

            if (eventPropertyValue is long)
            {
                return ((long)eventPropertyValue & this.signedRhsValue) == this.signedRhsValue;
            }
            else if (eventPropertyValue is ulong)
            {
                return ((ulong)eventPropertyValue & this.unsignedRhsValue) == this.unsignedRhsValue;
            }
            else if (eventPropertyValue is int)
            {
                return ((int)eventPropertyValue & this.signedRhsValue) == this.signedRhsValue;
            }
            else if (eventPropertyValue is uint)
            {
                return ((uint)eventPropertyValue & this.unsignedRhsValue) == this.unsignedRhsValue;
            }
            if (eventPropertyValue is short)
            {
                return ((short)eventPropertyValue & this.signedRhsValue) == this.signedRhsValue;
            }
            else if (eventPropertyValue is ushort)
            {
                return ((ushort)eventPropertyValue & this.unsignedRhsValue) == this.unsignedRhsValue;
            }
            else if (eventPropertyValue is sbyte)
            {
                return ((sbyte)eventPropertyValue & this.signedRhsValue) == this.signedRhsValue;
            }
            else if (eventPropertyValue is byte)
            {
                return ((byte)eventPropertyValue & this.unsignedRhsValue) == this.unsignedRhsValue;
            }

            return false;
        }
    }
}
