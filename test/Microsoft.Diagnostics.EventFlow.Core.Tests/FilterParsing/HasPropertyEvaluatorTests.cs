// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Diagnostics.EventFlow.FilterEvaluators;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests.FilterParsing
{
    public class HasPropertyEvaluatorTests
    {
        [Fact]
        public void MissingProperty()
        {
            var evaluator = new HasPropertyEvaluator("InvalidPropertyName");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ExistingProperty()
        {
            var evaluator = new HasPropertyEvaluator("EventId");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void HasPropertyEvaluatorParsing()
        {
            FilterParser parser = new FilterParser();

            var result = parser.Parse("hasproperty foo");
            Assert.Equal("(hasproperty foo)", result.SemanticsString);

            // A more complicated expression, just to make sure the operator meshes in well with the rest
            result = parser.Parse("(ActorId==1234) && hasproperty bravo || (beta > 14 && !(Level==Warning))");
            Assert.Equal("(((__EqualityEvaluator:ActorId==1234)AND(hasproperty bravo))OR((__GreaterThanEvaluator:beta>14)AND(NOT(__EqualityEvaluator:Level==Warning))))", result.SemanticsString);
        }
    }
}