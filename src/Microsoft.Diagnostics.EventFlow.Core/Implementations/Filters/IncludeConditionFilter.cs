// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Diagnostics.EventFlow.FilterEvaluators;

namespace Microsoft.Diagnostics.EventFlow.Filters
{
    public abstract class IncludeConditionFilter: IFilter
    {
        private string includeCondition;
        protected FilterEvaluator Evaluator;

        public IncludeConditionFilter(string includeCondition = null)
        {
            this.IncludeCondition = includeCondition;
        }

        public string IncludeCondition
        {
            get
            {
                return this.includeCondition;
            }

            set
            {
                var parser = new FilterParser();
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.Evaluator = PositiveEvaluator.Instance.Value; // Empty condition == include everything
                }
                else
                {
                    try
                    {
                        this.Evaluator = parser.Parse(value);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException($"Failed to parse filter condition: {value}", e);
                    }
                }
                this.includeCondition = value;
            }
        }

        public abstract FilterResult Evaluate(EventData eventData);

        public override bool Equals(object obj)
        {
            IncludeConditionFilter other = obj as IncludeConditionFilter;
            if (other == null)
            {
                return false;
            }

            return this.includeCondition == other.includeCondition;
        }

        public override int GetHashCode()
        {
            return this.includeCondition?.GetHashCode() ?? 0;
        }
    }
}
