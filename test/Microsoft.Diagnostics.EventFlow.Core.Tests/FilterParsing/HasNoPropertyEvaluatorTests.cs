using Microsoft.Diagnostics.EventFlow.FilterEvaluators;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests.FilterParsing
{
    public class HasNoPropertyEvaluatorTests
    {
        [Fact]
        public void MissingPropertyEquality()
        {
            var evaluator = new HasNoPropertyEvaluator("InvalidPropertyName");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ExistingPropertyEquality()
        {
            var evaluator = new HasNoPropertyEvaluator("EventId");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }
    }
}