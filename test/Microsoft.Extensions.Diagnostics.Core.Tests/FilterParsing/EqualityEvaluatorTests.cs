//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Diagnostics;
using Microsoft.Extensions.Diagnostics.FilterEvaluators;
using Xunit;

namespace Microsoft.VisualStudio.Azure.Fabric.DiagnosticEvents.UnitTests.FilterParsing
{
    public class EqualityEvaluatorTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void EqualityEvaluatorsBasicParsing()
        {
            // Can work with/without whitespace surrounded
            parser.Parse("prop==1");
            parser.Parse("prop == 1");

            // Accept unquoted value (plain text, date time, number)
            parser.Parse("prop == abw1032-c");
            parser.Parse("prop == 2015-05-29T10:45:00.537Z");
            parser.Parse("prop == \"text with double quote \\\" end\"");

            // Invalid expressions
            Assert.ThrowsAny<Exception>(() => parser.Parse("different"));
            Assert.ThrowsAny<Exception>(() => parser.Parse("prop == "));
            Assert.ThrowsAny<Exception>(() => parser.Parse(" == value"));
        }

        [Fact]
        public void IdPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("EventId", "1234");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("EventId", "-34");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("EventId", "notIntegerValue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MessagePropertyEquality()
        {
            // String comparison should be case-insensitive
            var evaluator = new EqualityEvaluator("Message", "test event with many properties of DIFFERENT types");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("Message", "not a match");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void TimestampPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("Timestamp", "2015-05-29T10:45:00.537Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same timestamp
            evaluator = new EqualityEvaluator("Timestamp", "2015-05-29T10:45:00.537+00:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("Timestamp", "2015-05-29T10:50:00Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MissingPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("InvalidPropertyName", "somevalue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void StringPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("StringProperty", "Ala ma kota");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("StringProperty", "Ala MA kota");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("StringProperty", "Kota ma Ala");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Leading or trailing whitespace is significant
            evaluator = new EqualityEvaluator("StringProperty", "Ala ma kota ");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("StringProperty", " Ala ma kota");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void IntPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("IntProperty", "-65000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("IntProperty", "65000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("IntProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Not an int value
            evaluator = new EqualityEvaluator("IntProperty", "5000000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void LongPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("LongProperty", "-5000000000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("LongProperty", "5000000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("LongProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("LongProperty", "not a long value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ShortPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("ShortProperty", "-18000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("ShortProperty", "18000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("ShortProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Not a short value
            evaluator = new EqualityEvaluator("ShortProperty", "80000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void SbytePropertyEquality()
        {
            var evaluator = new EqualityEvaluator("SbyteProperty", "-20");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("SbyteProperty", "20");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("SbyteProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Not a sbyte value
            evaluator = new EqualityEvaluator("SbyteProperty", "-333");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UintPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("UintProperty", "80000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("UintProperty", "-80000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("UintProperty", "80001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("UintProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("UintProperty", "not an uint value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UlongPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("UlongProperty", "5100000000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("UlongProperty", "-5100000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("UlongProperty", "5100000001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("UlongProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("UlongProperty", "not an ulong value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UshortPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("UshortProperty", "18200");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("UshortProperty", "-18200");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("UshortProperty", "18199");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("UshortProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Not an ushort value
            evaluator = new EqualityEvaluator("UshortProperty", "-7");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BytePropertyEquality()
        {
            var evaluator = new EqualityEvaluator("ByteProperty", "7");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("ByteProperty", "8");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("ByteProperty", "-3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("ByteProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("ByteProperty", "not a byte value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void FloatPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("FloatProperty", "-7.4");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different representation of the same number
            evaluator = new EqualityEvaluator("FloatProperty", "-74E-1");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("FloatProperty", "74E-1");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("FloatProperty", "-7.5");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("FloatProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("FloatProperty", "not a float value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of float range
            evaluator = new EqualityEvaluator("FloatProperty", "-2.347E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DoublePropertyEquality()
        {
            var evaluator = new EqualityEvaluator("DoubleProperty", "-2.347E43");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different representation of the same number
            evaluator = new EqualityEvaluator("DoubleProperty", "-234.7E41");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("DoubleProperty", "2.347E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("DoubleProperty", "-2.3471E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("DoubleProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("DoubleProperty", "not a double value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimePropertyEquality()
        {
            var evaluator = new EqualityEvaluator("DateTimeProperty", "2015-03-30T09:15:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same value
            evaluator = new EqualityEvaluator("DateTimeProperty", "2015-03-30 09:15:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different value
            evaluator = new EqualityEvaluator("DateTimeProperty", "2015-03-30T09:15:01");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("DateTimeProperty", "not a DateTime value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimeOffsetPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same value
            evaluator = new EqualityEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485+00:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different time zone
            evaluator = new EqualityEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485+00:01");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different value
            evaluator = new EqualityEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("DateTimeOffsetProperty", "not a DateTimeOffset value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BoolPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("BoolProperty", "true");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("BoolProperty", "false");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("BoolProperty", "not a bool value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void GuidPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("GuidProperty", "8DCE9920-E985-4B63-8ECE-A22160421FA3");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("GuidProperty", "{8DCE9920-E985-4B63-8ECE-A22160421FA3}");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("GuidProperty", "67FED6E7-23C5-45DB-96DE-F763E169C922");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new EqualityEvaluator("GuidProperty", "not a GUID value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }
    }
}
