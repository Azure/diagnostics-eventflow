//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics;
using Microsoft.Extensions.Diagnostics.FilterEvaluators;
using Xunit;

namespace Microsoft.VisualStudio.Azure.Fabric.DiagnosticEvents.UnitTests.FilterParsing
{
    // The InequalityEvaluator is a very simple modification of the EqualityEvaluator. Therefore, the set of tests
    // for the InequalityEvaluator is limited, meant to be a "smoke test" set rather than something more comprehensive.
    // If ever the implementation of InequalityEvaluator changes, more tests will need to be added 
    // (see EqualityEvaluatorTests for inspiration).
    public class InequalityEvaluatorTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void IdPropertyInequality()
        {
            var evaluator = new InequalityEvaluator("EventId", "1234");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new InequalityEvaluator("EventId", "-34");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new InequalityEvaluator("EventId", "notIntegerValue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MessagePropertyInequality()
        {
            // String comparison should be case-insensitive
            var evaluator = new InequalityEvaluator("Message", "test event with many properties of DIFFERENT types");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new InequalityEvaluator("Message", "not a match");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void TimestampPropertyInequality()
        {
            var evaluator = new InequalityEvaluator("Timestamp", "2015-05-29T10:45:00.537Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new InequalityEvaluator("Timestamp", "2015-05-29T10:50:00Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MissingPropertyInequality()
        {
            var evaluator = new InequalityEvaluator("InvalidPropertyName", "somevalue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void StringPropertyInequality()
        {
            var evaluator = new InequalityEvaluator("StringProperty", "Ala ma kota");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new InequalityEvaluator("StringProperty", "Kota ma Ala");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void IntPropertyInequality()
        {
            var evaluator = new InequalityEvaluator("IntProperty", "-65000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new InequalityEvaluator("IntProperty", "65000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new InequalityEvaluator("IntProperty", "5000000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimePropertyInequality()
        {
            var evaluator = new InequalityEvaluator("DateTimeProperty", "2015-03-30T09:15:00");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different value
            evaluator = new InequalityEvaluator("DateTimeProperty", "2015-03-30T09:15:01");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new InequalityEvaluator("DateTimeProperty", "not a DateTime value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void GuidPropertyInequality()
        {
            var evaluator = new InequalityEvaluator("GuidProperty", "8DCE9920-E985-4B63-8ECE-A22160421FA3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new InequalityEvaluator("GuidProperty", "{8DCE9920-E985-4B63-8ECE-A22160421FA3}");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new InequalityEvaluator("GuidProperty", "67FED6E7-23C5-45DB-96DE-F763E169C922");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new InequalityEvaluator("GuidProperty", "not a GUID value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }
    }
}
