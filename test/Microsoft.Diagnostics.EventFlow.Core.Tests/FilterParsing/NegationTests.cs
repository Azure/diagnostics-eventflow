//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Diagnostics.EventFlow;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests.FilterParsing
{
    public class NegationTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void NegationBasicParsing()
        {
            parser.Parse("!(prop==different)");
            parser.Parse("! (prop==different)");

            // Negation must be braced. And there no whitespace is allowed between negation and brace.
            Assert.ThrowsAny<Exception>(() => parser.Parse("!prop==different"));
        }

        [Fact]
        public void NegationWithSimpleTerm()
        {
            var result = parser.Parse("!(prop == \"blahblah\") ");
            Assert.True(result.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // EventId equals to 1234 in ManyPropertiesEvent
            result = parser.Parse(" !(EventId == 1234) ");
            Assert.False(result.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void NegationWithCompositeTerm()
        {
            var result = parser.Parse("!((IntProperty == -65000 || ByteProperty >8) && prop == blahblah)");
            // There is no blahblah in the ManyPropertiesEvent, so the evaluation should yield true
            Assert.True(result.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Now 'not' applies just to the first (composite) term. The IntProperty _is_ -65000, so the whole thing should evaluate to false
            result = parser.Parse("!(IntProperty == -65000 || ByteProperty >8) || prop==blahblah");
            Assert.False(result.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // ByteProperty is less than 8, so the following is true
            result = parser.Parse("!(IntProperty == -65000 && ByteProperty >8) ");
            Assert.True(result.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void NegationWithPropertyExpressions()
        {
            var result = parser.Parse("!(IntProperty == -65000) ");
            Assert.False(result.Evaluate(FilteringTestData.ManyPropertiesEvent));

            result = parser.Parse("!(DateTimeProperty > 2015-03-30T09:15:00)");
            Assert.True(result.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void LogicalOperatorPriority()
        {
            var result = parser.Parse("!(prop==alpha) && !(prop==bravo) || !(prop==charlie) || !(prop==delta) && prop==echo");
            Assert.Equal("((((NOT(__EqualityEvaluator:prop==alpha))AND(NOT(__EqualityEvaluator:prop==bravo)))OR(NOT(__EqualityEvaluator:prop==charlie)))OR((NOT(__EqualityEvaluator:prop==delta))AND(__EqualityEvaluator:prop==echo)))", result.SemanticsString);

            result = parser.Parse("!(prop==alpha) || (!(prop==bravo) || !(prop==charlie) && !(prop==delta)) && prop==echo");
            Assert.Equal("((NOT(__EqualityEvaluator:prop==alpha))OR(((NOT(__EqualityEvaluator:prop==bravo))OR((NOT(__EqualityEvaluator:prop==charlie))AND(NOT(__EqualityEvaluator:prop==delta))))AND(__EqualityEvaluator:prop==echo)))", result.SemanticsString);
        }
    }
}
