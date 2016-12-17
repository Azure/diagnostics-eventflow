// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Diagnostics.EventFlow;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests.FilterParsing
{
    public class QuotedTextTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void QuotedText()
        {
            var result = parser.Parse("prop==\"bravo\"");
            Assert.Equal("(__EqualityEvaluator:prop==bravo)", result.SemanticsString);
        }

        [Fact]
        public void QuotedTextWithWhitespaceAround()
        {
            var result = parser.Parse("prop ==  \"bravo\"  ");
            Assert.Equal("(__EqualityEvaluator:prop==bravo)", result.SemanticsString);
        }

        [Fact]
        public void QuotedTextWithWhitespaceInside()
        {
            var result = parser.Parse(" prop == \"bra v o\"  ");
            Assert.Equal("(__EqualityEvaluator:prop==bra v o)", result.SemanticsString);
        }

        [Fact]
        public void QuotedTextWithQuoteChars()
        {
            var result = parser.Parse(" prop == \"bra\\\"vo\\\"\"  ");
            Assert.Equal("(__EqualityEvaluator:prop==bra\"vo\")", result.SemanticsString);
        }

        [Fact]
        public void QuotedTextWithNonWordChars()
        {
            var result = parser.Parse(" prop == \":bra(v}o\"  ");
            Assert.Equal("(__EqualityEvaluator:prop==:bra(v}o)", result.SemanticsString);
        }

        [Fact]
        public void UnmatchedQuote()
        {
            Assert.ThrowsAny<Exception>(() => parser.Parse("prop == bra\"vo"));

            Assert.ThrowsAny<Exception>(() => parser.Parse("prop == \"bravo"));

            Assert.ThrowsAny<Exception>(() => parser.Parse("prop == bravo\""));
        }
    }
}
