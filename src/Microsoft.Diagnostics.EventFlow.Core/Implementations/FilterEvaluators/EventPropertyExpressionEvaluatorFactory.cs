//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal static class EventPropertyExpressionEvaluatorFactory
    {
        public static EventPropertyExpressionEvaluator CreateEvaluator(string propertyName, string op, string value)
        {
            switch (op)
            {
                case "==":
                    return new EqualityEvaluator(propertyName, value);

                case ">":
                    return new GreaterThanEvaluator(propertyName, value);

                case "!=":
                    return new InequalityEvaluator(propertyName, value);

                case "<":
                    return new LessThanEvaluator(propertyName, value);

                case ">=":
                    return new GreaterOrEqualsEvaluator(propertyName, value);

                case "<=":
                    return new LessOrEqualsEvaluator(propertyName, value);

                case "~=":
                    return new RegexEvaluator(propertyName, value);

                default:
                    throw new ArgumentException("Unknown operator", nameof(op));
            }
        }
    }
}
