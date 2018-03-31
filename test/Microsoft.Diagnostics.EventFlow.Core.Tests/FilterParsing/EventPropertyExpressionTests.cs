// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Diagnostics.EventFlow;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests.FilterParsing
{
    public class EventPropertyExpressionTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void BooleanProperty()
        {
            var result = parser.Parse(" delta==true");
            Assert.Equal("(__EqualityEvaluator:delta==true)", result.SemanticsString);
        }

        [Fact]
        public void IntegerProperty()
        {
            var result = parser.Parse("ActorId==23");
            Assert.Equal("(__EqualityEvaluator:ActorId==23)", result.SemanticsString);

            result = parser.Parse("ActorId == -317");
            Assert.Equal("(__EqualityEvaluator:ActorId==-317)", result.SemanticsString);
        }

        [Fact]
        public void DoubleProperty()
        {
            var result = parser.Parse(" MemoryPressure == 1.25E3");
            Assert.Equal("(__EqualityEvaluator:MemoryPressure==1.25E3)", result.SemanticsString);
        }

        [Fact]
        public void StringProperty()
        {
            var result = parser.Parse("Level==Error");
            Assert.Equal("(__EqualityEvaluator:Level==Error)", result.SemanticsString);

            result = parser.Parse("  Message == \"Unexpected error occurred while provisioning the service\"");
            Assert.Equal("(__EqualityEvaluator:Message==Unexpected error occurred while provisioning the service)", result.SemanticsString);
        }

        [Fact]
        public void TimestampProperty()
        {
            var result = parser.Parse(" Timestamp == 2015-04-24T11:02:30  ");
            Assert.Equal("(__EqualityEvaluator:Timestamp==2015-04-24T11:02:30)", result.SemanticsString);
        }

        [Fact]
        public void GuidProperty()
        {
            var result = parser.Parse("PartitionID==aa4ec4af-adcc-49fe-ab44-63786f898088");
            Assert.Equal("(__EqualityEvaluator:PartitionID==aa4ec4af-adcc-49fe-ab44-63786f898088)", result.SemanticsString);
        }

        [Fact]
        public void WhitespaceAroundOperator()
        {
            var result = parser.Parse(" Counter >  234");
            Assert.Equal("(__GreaterThanEvaluator:Counter>234)", result.SemanticsString);
        }

        [Fact]
        public void GreaterThanOperator()
        {
            var result = parser.Parse(" delta>17 ");
            Assert.Equal("(__GreaterThanEvaluator:delta>17)", result.SemanticsString);

            result = parser.Parse("delta > 17");
            Assert.Equal("(__GreaterThanEvaluator:delta>17)", result.SemanticsString);
        }

        [Fact]
        public void LessThanOperator()
        {
            var result = parser.Parse(" delta<17 ");
            Assert.Equal("(__LessThanEvaluator:delta<17)", result.SemanticsString);

            result = parser.Parse("delta < 17");
            Assert.Equal("(__LessThanEvaluator:delta<17)", result.SemanticsString);
        }

        [Fact]
        public void GreaterOrEqualsOperator()
        {
            var result = parser.Parse(" delta>=17 ");
            Assert.Equal("(__GreaterOrEqualsEvaluator:delta>=17)", result.SemanticsString);

            result = parser.Parse("delta >= 17");
            Assert.Equal("(__GreaterOrEqualsEvaluator:delta>=17)", result.SemanticsString);
        }

        [Fact]
        public void LessOrEqualsOperator()
        {
            var result = parser.Parse(" delta<=17 ");
            Assert.Equal("(__LessOrEqualsEvaluator:delta<=17)", result.SemanticsString);

            result = parser.Parse("delta <= 17");
            Assert.Equal("(__LessOrEqualsEvaluator:delta<=17)", result.SemanticsString);
        }

        [Fact]
        public void InequalityOperator()
        {
            var result = parser.Parse(" delta!=17 ");
            Assert.Equal("(__InequalityEvaluator:delta!=17)", result.SemanticsString);

            result = parser.Parse("delta != 17");
            Assert.Equal("(__InequalityEvaluator:delta!=17)", result.SemanticsString);
        }

        [Fact]
        public void RegexOperator()
        {
            var result = parser.Parse(" delta~=ala ");
            Assert.Equal("(__RegexEvaluator:delta~=ala)", result.SemanticsString);

            result = parser.Parse("delta ~= ma");
            Assert.Equal("(__RegexEvaluator:delta~=ma)", result.SemanticsString);

            result = parser.Parse("delta~=\"\\*+?|{[()^$.#\"");
            Assert.Equal("(__RegexEvaluator:delta~=\\*+?|{[()^$.#)", result.SemanticsString);
        }

        [Fact]
        public void BinaryAndOperator()
        {
            var result = parser.Parse(" Keywords&==321 ");
            Assert.Equal("(__BitwiseEqualityEvaluator:Keywords&==321)", result.SemanticsString);

            result = parser.Parse("Keywords &== 321");
            Assert.Equal("(__BitwiseEqualityEvaluator:Keywords&==321)", result.SemanticsString);

            result = parser.Parse("Keywords &== 0xFF");
            Assert.Equal("(__BitwiseEqualityEvaluator:Keywords&==0xFF)", result.SemanticsString);

            result = parser.Parse("!(Keywords &== 0xFF)");
            Assert.Equal("(NOT(__BitwiseEqualityEvaluator:Keywords&==0xFF))", result.SemanticsString);

            Assert.Throws<ArgumentException>(() => parser.Parse("Keywords &== abc"));
        }

        [Fact]
        public void InvalidOperatorSpacing()
        {
            Assert.ThrowsAny<Exception>(() => parser.Parse(" delta> =17 "));

            Assert.ThrowsAny<Exception>(() => parser.Parse(" delta< =17 "));

            Assert.ThrowsAny<Exception>(() => parser.Parse(" delta! =17 "));

            Assert.ThrowsAny<Exception>(() => parser.Parse(" delta~ =17 "));
        }
    }
}
