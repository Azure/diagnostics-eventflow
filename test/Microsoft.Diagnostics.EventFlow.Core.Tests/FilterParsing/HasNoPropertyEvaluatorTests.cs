// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Diagnostics.EventFlow.FilterEvaluators;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests.FilterParsing
{
    public class HasNoPropertyEvaluatorTests
    {
        [Fact]
        public void MissingProperty()
        {
            var evaluator = new HasNoPropertyEvaluator("InvalidPropertyName");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ExistingProperty()
        {
            var evaluator = new HasNoPropertyEvaluator("EventId");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void HasNoPropertyEvaluatorParsing()
        {
            FilterParser parser = new FilterParser();

            var result = parser.Parse("hasnoproperty foo");
            Assert.Equal("(hasnoproperty foo)", result.SemanticsString);

            // A more complicated expression, just to make sure the operator meshes in well with the rest
            result = parser.Parse("(ActorId==1234) && hasnoproperty bravo || (beta > 14 && !(Level==Warning))");
            Assert.Equal("(((__EqualityEvaluator:ActorId==1234)AND(hasnoproperty bravo))OR((__GreaterThanEvaluator:beta>14)AND(NOT(__EqualityEvaluator:Level==Warning))))", result.SemanticsString);
        }
    }
}