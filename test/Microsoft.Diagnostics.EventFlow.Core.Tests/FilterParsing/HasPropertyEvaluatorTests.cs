using Microsoft.Diagnostics.EventFlow.FilterEvaluators;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests.FilterParsing
{
    public class NullPropertyEvaluatorTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void MissingPropertyEquality()
        {
            var evaluator = new HasPropertyEvaluator("InvalidPropertyName");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ExistingPropertyEquality()
        {
            var evaluator = new HasPropertyEvaluator("EventId");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }
    }
}