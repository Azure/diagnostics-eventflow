// ------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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

                case "&==":
                    return new BitwiseEqualityEvaluator(propertyName, value);

                case "is":
                    return new NullPropertyEvaluator(propertyName, value);

                default:
                    throw new ArgumentException("Unknown operator", nameof(op));
            }
        }
    }
}
