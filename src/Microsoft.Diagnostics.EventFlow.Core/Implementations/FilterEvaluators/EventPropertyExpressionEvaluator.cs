//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal abstract class EventPropertyExpressionEvaluator : FilterEvaluator
    {
        // Property name and value, passed by the filter expression parser
        protected string propertyName;
        protected string value;

        public EventPropertyExpressionEvaluator(string propertyName, string value)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException("Property name must not be empty", nameof(propertyName));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value must not be empty", nameof(value));
            }

            this.propertyName = propertyName;
            this.value = value;
        }
    }
}
