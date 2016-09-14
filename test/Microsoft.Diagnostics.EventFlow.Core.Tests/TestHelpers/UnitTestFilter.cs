// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.FilterEvaluators;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class UnitTestFilter : IFilter
    {
        public TimeSpan EvaluationDelay = TimeSpan.Zero;
        private FilterEvaluator failureConditionEvaluator = new NegationEvaluator(PositiveEvaluator.Instance.Value);

        public string EvaluationFailureCondition
        {
            set
            {
                var parser = new FilterParser();
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.failureConditionEvaluator = new NegationEvaluator(PositiveEvaluator.Instance.Value); // Empty condition == no failures
                }
                else
                {
                    try
                    {
                        this.failureConditionEvaluator = parser.Parse(value);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException($"Failed to parse filter condition: {value}", e);
                    }
                }
            }
        }

        public FilterResult Evaluate(EventData eventData)
        {
            if (this.EvaluationDelay != TimeSpan.Zero)
            {
                Thread.Sleep(EvaluationDelay);
            }

            if (this.failureConditionEvaluator.Evaluate(eventData))
            {
                throw new Exception("This event is bad!");
            }

            return FilterResult.KeepEvent;
        }
    }

    public class UnitTestFilterFactory : IPipelineItemFactory<UnitTestFilter>
    {
        public UnitTestFilter CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            return new UnitTestFilter();
        }
    }
}
