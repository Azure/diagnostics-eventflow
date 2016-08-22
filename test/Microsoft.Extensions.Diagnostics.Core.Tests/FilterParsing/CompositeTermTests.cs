//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Diagnostics;
using Xunit;

namespace Microsoft.VisualStudio.Azure.Fabric.DiagnosticEvents.UnitTests.FilterParsing
{
    public class CompositeTermTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void StandaloneCompositeTerm()
        {
            var result = parser.Parse("(prop==echo)");
            Assert.Equal("(__EqualityEvaluator:prop==echo)", result.SemanticsString);

            result = parser.Parse(" (  prop == echo ) ");
            Assert.Equal("(__EqualityEvaluator:prop==echo)", result.SemanticsString);

            result = parser.Parse(" (prop == \"echo echo\" ) ");
            Assert.Equal("(__EqualityEvaluator:prop==echo echo)", result.SemanticsString);

            result = parser.Parse(" (Counter >  234) ");
            Assert.Equal("(__GreaterThanEvaluator:Counter>234)", result.SemanticsString);
        }

        [Fact]
        public void NegatedCompositeTerm()
        {
            var result = parser.Parse("!(prop==echo)");
            Assert.Equal("(NOT(__EqualityEvaluator:prop==echo))", result.SemanticsString);

            result = parser.Parse("!( prop == echo ) ");
            Assert.Equal("(NOT(__EqualityEvaluator:prop==echo))", result.SemanticsString);

            result = parser.Parse("!(prop == \"echo echo\")");
            Assert.Equal("(NOT(__EqualityEvaluator:prop==echo echo))", result.SemanticsString);

            result = parser.Parse("!(Counter <  234) ");
            Assert.Equal("(NOT(__LessThanEvaluator:Counter<234))", result.SemanticsString);
        }

        [Fact]
        public void AlternativeWithCompositeTerm()
        {
            var result = parser.Parse("(ActorId==1234) || prop==bravo");
            Assert.Equal("((__EqualityEvaluator:ActorId==1234)OR(__EqualityEvaluator:prop==bravo))", result.SemanticsString);

            result = parser.Parse(" ActorId == 1234 || (PartitionID==aa4ec4af-adcc-49fe-ab44-63786f898088)");
            Assert.Equal("((__EqualityEvaluator:ActorId==1234)OR(__EqualityEvaluator:PartitionID==aa4ec4af-adcc-49fe-ab44-63786f898088))", result.SemanticsString);

            result = parser.Parse(" (prop==echo) || (PartitionID==aa4ec4af-adcc-49fe-ab44-63786f898088)");
            Assert.Equal("((__EqualityEvaluator:prop==echo)OR(__EqualityEvaluator:PartitionID==aa4ec4af-adcc-49fe-ab44-63786f898088))", result.SemanticsString);
        }

        [Fact]
        public void ConjunctionWithCompositeTerm()
        {
            var result = parser.Parse("(ActorId==1234) && prop==bravo");
            Assert.Equal("((__EqualityEvaluator:ActorId==1234)AND(__EqualityEvaluator:prop==bravo))", result.SemanticsString);

            result = parser.Parse(" ActorId==1234 && (PartitionID==aa4ec4af-adcc-49fe-ab44-63786f898088)");
            Assert.Equal("((__EqualityEvaluator:ActorId==1234)AND(__EqualityEvaluator:PartitionID==aa4ec4af-adcc-49fe-ab44-63786f898088))", result.SemanticsString);

            result = parser.Parse(" (prop==echo) && (PartitionID==aa4ec4af-adcc-49fe-ab44-63786f898088)");
            Assert.Equal("((__EqualityEvaluator:prop==echo)AND(__EqualityEvaluator:PartitionID==aa4ec4af-adcc-49fe-ab44-63786f898088))", result.SemanticsString);
        }

        [Fact]
        public void AllLogicalOperatorsWithCompositeTerms()
        {
            var result = parser.Parse("(ActorId==1234) && prop==bravo || (beta > 14 && !(Level==Warning))");
            Assert.Equal("(((__EqualityEvaluator:ActorId==1234)AND(__EqualityEvaluator:prop==bravo))OR((__GreaterThanEvaluator:beta>14)AND(NOT(__EqualityEvaluator:Level==Warning))))", result.SemanticsString);
        }

        [Fact]
        public void NestedCompositeTerms()
        {
            var result = parser.Parse("((ActorId == 1234) && (( prop == bravo)))");
            Assert.Equal("((__EqualityEvaluator:ActorId==1234)AND(__EqualityEvaluator:prop==bravo))", result.SemanticsString);
        }

        [Fact]
        public void InvalidCompositeTerm()
        {
            // Missing opening parethesis
            Assert.ThrowsAny<Exception>(() => parser.Parse(" ActorId == 1234) || prop == bravo"));

            Assert.ThrowsAny<Exception>(() => parser.Parse(" (ActorId == 1234) || prop == bravo)"));

            Assert.ThrowsAny<Exception>(() => parser.Parse(" (((prop == bravo)) ))"));

            // Missing closing parethesis
            Assert.ThrowsAny<Exception>(() => parser.Parse(" ((ActorId == 1234) || prop == bravo"));

            Assert.ThrowsAny<Exception>(() => parser.Parse(" (ActorId == 1234 || prop == bravo"));

            Assert.ThrowsAny<Exception>(() => parser.Parse(" ((( (((prop == bravo)) ))"));

            // Empty term
            Assert.ThrowsAny<Exception>(() => parser.Parse(" ( )"));

            Assert.ThrowsAny<Exception>(() => parser.Parse("prop == bravo && ()"));

            Assert.ThrowsAny<Exception>(() => parser.Parse(" (((( ))))"));
        }
    }
}
