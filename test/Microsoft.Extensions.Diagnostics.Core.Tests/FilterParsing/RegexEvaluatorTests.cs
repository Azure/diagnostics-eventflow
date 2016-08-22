//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using Microsoft.Extensions.Diagnostics;
using Microsoft.Extensions.Diagnostics.FilterEvaluators;
using Xunit;

namespace Microsoft.VisualStudio.Azure.Fabric.DiagnosticEvents.UnitTests.FilterParsing
{
    public class RegexEvaluatorTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void InvalidRegex()
        {
            var evaluator = new RegexEvaluator("Message", "*");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void IdProperty()
        {
            // Numeric properties should never match and Id should be no different
            var evaluator = new RegexEvaluator("Id", "1234");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MessageProperty()
        {
            // String comparison should be case-insensitive
            var evaluator = new RegexEvaluator("Message", "test event with many properties of DIFFERENT types");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("Message", "^Te.*proper");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("Message", "not a match");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void TimestampProperty()
        {
            var evaluator = new RegexEvaluator("Timestamp", "2015-05-29T10:45:00.537");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("Timestamp", "\\p{N}{4}-05");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("Timestamp", "00.536");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MissingProperty()
        {
            var evaluator = new RegexEvaluator("InvalidPropertyName", "somevalue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Invalid regex and missing property--still just no match, no exception
            evaluator = new RegexEvaluator("InvalidPropertyName", "*");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void StringProperty()
        {
            var evaluator = new RegexEvaluator("StringProperty", "Ala ma kota");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("StringProperty", "Ala MA kota");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("StringProperty", "Kotax");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Leading or trailing whitespace is significant
            evaluator = new RegexEvaluator("StringProperty", "Ala ma kota ");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("StringProperty", " Ala ma kota");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void IntProperty()
        {
            // Numeric properties should never match
            var evaluator = new RegexEvaluator("IntProperty", "-65000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Not an int value
            evaluator = new RegexEvaluator("IntProperty", "5000000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void LongProperty()
        {
            // Numeric properties should never match
            var evaluator = new RegexEvaluator("LongProperty", "-5000000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("LongProperty", "not a long value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ShortProperty()
        {
            // Numeric properties should never match
            var evaluator = new RegexEvaluator("ShortProperty", "-18000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Not a short value
            evaluator = new RegexEvaluator("ShortProperty", "80000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void SbyteProperty()
        {
            // Numeric properties should never match
            var evaluator = new RegexEvaluator("SbyteProperty", "-20");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Not a sbyte value
            evaluator = new RegexEvaluator("SbyteProperty", "-333");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UintProperty()
        {
            // Numeric properties should never match
            var evaluator = new RegexEvaluator("UintProperty", "80000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("UintProperty", "not an uint value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UlongProperty()
        {
            // Numeric properties should never match
            var evaluator = new RegexEvaluator("UlongProperty", "5100000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("UlongProperty", "not an ulong value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UshortProperty()
        {
            // Numeric properties should never match
            var evaluator = new RegexEvaluator("UshortProperty", "18200");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Not an ushort value
            evaluator = new RegexEvaluator("UshortProperty", "-7");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ByteProperty()
        {
            // Numeric properties should never match
            var evaluator = new RegexEvaluator("ByteProperty", "7");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("ByteProperty", "not a byte value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void FloatProperty()
        {
            // Numeric properties should never match
            var evaluator = new RegexEvaluator("FloatProperty", "-7.4");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("FloatProperty", "not a float value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DoubleProperty()
        {
            // Numeric properties should never match
            var evaluator = new RegexEvaluator("DoubleProperty", "-2.347E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("DoubleProperty", "not a double value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimeProperty()
        {
            var evaluator = new RegexEvaluator("DateTimeProperty", "2015-03-30T09:15:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("DateTimeProperty", "..:15");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("DateTimeProperty", "40");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimeOffsetProperty()
        {
            var evaluator = new RegexEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same value
            evaluator = new RegexEvaluator("DateTimeOffsetProperty", "05-29");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("DateTimeOffsetProperty", "ZZ");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BoolProperty()
        {
            // Boolean properties should never mach
            var evaluator = new RegexEvaluator("BoolProperty", "true");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("BoolProperty", "not a bool value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void GuidProperty()
        {
            var evaluator = new RegexEvaluator("GuidProperty", "8DCE9920-E985-4B63-8ECE-A22160421FA3");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("GuidProperty", "8dce");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("GuidProperty", "8dcf");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new RegexEvaluator("GuidProperty", "not a GUID value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void TimeoutIsNoMatch()
        {
            var evaluator = new RegexEvaluator("AbaShortProperty", "a(.+)*ba");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // This should also be a match, but due to backtracking it takes a super-long time for the regex engine to analyze the value of this property.
            // Our evaluator will time out and report no match.
            evaluator = new RegexEvaluator("AbaLongProperty", "a(.+)*ba");
            Stopwatch s = Stopwatch.StartNew();
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            TimeSpan padding = TimeSpan.FromSeconds(2);
            Assert.True(s.Elapsed < RegexEvaluator.MatchTimeout + padding);
        }
    }
}
