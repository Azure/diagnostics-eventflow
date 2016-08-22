//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Diagnostics;
using Xunit;

namespace Microsoft.VisualStudio.Azure.Fabric.DiagnosticEvents.UnitTests.FilterParsing
{
    public class ConjunctionTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void ConjunctionBasicParsing()
        {
            // Can work with/without whitespace surrounded
            parser.Parse("prop==1&&prop==2");
            parser.Parse("prop==1 && prop==2");

            // Both side must be an expression
            Assert.ThrowsAny<Exception>(() => parser.Parse("different && \"ala\" "));
            Assert.ThrowsAny<Exception>(() => parser.Parse("prop==different && \"ala\" "));
            Assert.ThrowsAny<Exception>(() => parser.Parse("different && prop==\"ala\" "));
        }

        [Fact]
        public void ConjunctionWithCompositeTerms()
        {
            var result = parser.Parse("FloatProperty == -7.4 && (ByteProperty > 6 || UlongProperty == 0)");
            Assert.True(result.Evaluate(FilteringTestData.ManyPropertiesEvent));

            result = parser.Parse("FloatProperty == -7.4 && (ByteProperty >= 7 || UlongProperty == 0)");
            Assert.True(result.Evaluate(FilteringTestData.ManyPropertiesEvent));

            result = parser.Parse("(xxx==3 || yyy==5) && Id==1234");
            Assert.False(result.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ConjunctionWithPropertyExpressions()
        {
            var result = parser.Parse("FloatProperty == -7.4 && ByteProperty > 6");
            Assert.True(result.Evaluate(FilteringTestData.ManyPropertiesEvent));

            result = parser.Parse("FloatProperty == -7.4 && ByteProperty > 8");
            Assert.False(result.Evaluate(FilteringTestData.ManyPropertiesEvent));

            result = parser.Parse("FloatProperty == -7.4 && ByteProperty < 8");
            Assert.True(result.Evaluate(FilteringTestData.ManyPropertiesEvent));

            result = parser.Parse("FloatProperty == -7.4 && ByteProperty >= 8");
            Assert.False(result.Evaluate(FilteringTestData.ManyPropertiesEvent));

            result = parser.Parse("FloatProperty == -7.4 && ByteProperty <= 8");
            Assert.True(result.Evaluate(FilteringTestData.ManyPropertiesEvent));

            result = parser.Parse("Message ~= \"with many\" && ByteProperty <= 8");
            Assert.True(result.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }
    }
}
