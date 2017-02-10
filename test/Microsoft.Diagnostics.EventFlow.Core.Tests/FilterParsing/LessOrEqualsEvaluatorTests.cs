// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.FilterEvaluators;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests.FilterParsing
{
    public class LessOrEqualsEvaluatorTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void IdPropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("EventId", "1234");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("EventId", "-34");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("EventId", "1235");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("EventId", "notIntegerValue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MessagePropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("Message", "Test event with many properties of different types");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("Message", "aaa");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("Message", "zzz");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void TimestampPropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("Timestamp", "2015-05-29T10:45:00.537Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same timestamp
            evaluator = new LessOrEqualsEvaluator("Timestamp", "2015-05-29T10:45:00.537+00:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("Timestamp", "2015-05-29T10:50:00Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("Timestamp", "2015-05-29T10:45:00Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("Timestamp", "not a timestamp value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MissingPropertyLessOrEquals()
        {
            var evaluator = new LessOrEqualsEvaluator("InvalidPropertyName", "somevalue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void StringPropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("StringProperty", "Ala ma kota");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Comparison should be case-insensitive
            evaluator = new LessOrEqualsEvaluator("StringProperty", "Ala MA kota");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Greater value
            evaluator = new LessOrEqualsEvaluator("StringProperty", "Kota ma Ala");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("StringProperty", "AAla");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void IntPropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("IntProperty", "-65000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("IntProperty", "65000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("IntProperty", "-65001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("IntProperty", "not an int value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void LongPropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("LongProperty", "-5000000000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("LongProperty", "5000000000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("LongProperty", "-5000000001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessOrEqualsEvaluator("LongProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("LongProperty", "not a long value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ShortPropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("ShortProperty", "-18000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("ShortProperty", "18000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("ShortProperty", "-18001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessOrEqualsEvaluator("ShortProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for short values
            evaluator = new LessOrEqualsEvaluator("ShortProperty", "70000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void SbytePropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("SbyteProperty", "-20");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("SbyteProperty", "20");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("SbyteProperty", "-21");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessOrEqualsEvaluator("SbyteProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new LessOrEqualsEvaluator("SbyteProperty", "234");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UintPropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("UintProperty", "80000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("UintProperty", "80001");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("UintProperty", "70500");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessOrEqualsEvaluator("UintProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for uint
            evaluator = new LessOrEqualsEvaluator("UintProperty", "-80000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UlongPropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("UlongProperty", "5100000000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("UlongProperty", "5100000001");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("UlongProperty", "5000000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessOrEqualsEvaluator("UlongProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new LessOrEqualsEvaluator("UlongProperty", "-5100000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UshortPropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("UshortProperty", "18200");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("UshortProperty", "18201");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("UshortProperty", "18199");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessOrEqualsEvaluator("UshortProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new LessOrEqualsEvaluator("UshortProperty", "-18200");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BytePropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("ByteProperty", "7");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("ByteProperty", "8");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("ByteProperty", "3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessOrEqualsEvaluator("ByteProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for byte
            evaluator = new LessOrEqualsEvaluator("ByteProperty", "-3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void FloatPropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("FloatProperty", "-7.4");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different representation of the same number
            evaluator = new LessOrEqualsEvaluator("FloatProperty", "-74E-1");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("FloatProperty", "74E-1");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("FloatProperty", "-7.5");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessOrEqualsEvaluator("FloatProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new LessOrEqualsEvaluator("FloatProperty", "-2.347E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DoublePropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("DoubleProperty", "-2.347E43");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different representation of the same number
            evaluator = new LessOrEqualsEvaluator("DoubleProperty", "-234.7E41");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("DoubleProperty", "2.347E43");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("DoubleProperty", "-2.3471E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessOrEqualsEvaluator("DoubleProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("DoubleProperty", "not a double value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimePropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("DateTimeProperty", "2015-03-30T09:15:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same value
            evaluator = new LessOrEqualsEvaluator("DateTimeProperty", "2015-03-30 09:15:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("DateTimeProperty", "2015-03-30T09:15:01");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("DateTimeProperty", "2015-03-30T09:14:59");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("DateTimeProperty", "not a DateTime value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimeOffsetPropertyLessOrEquals()
        {
            // Same value
            var evaluator = new LessOrEqualsEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same value
            evaluator = new LessOrEqualsEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485+00:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value in different time zone
            evaluator = new LessOrEqualsEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485+00:01");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessOrEqualsEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessOrEqualsEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:18Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("DateTimeOffsetProperty", "not a DateTimeOffset value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BoolPropertyLessOrEquals()
        {
            // Bool properties do not work with less-or-equals operator, so no matter what RHS value we use the evaluation should always be false.
            var evaluator = new LessOrEqualsEvaluator("BoolProperty", "true");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("BoolProperty", "false");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("BoolProperty", "not a bool value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void GuidPropertyLessOrEquals()
        {
            // GUID properties do not work with less-or-equals operator, so no matter what RHS value we use the evaluation should always be false.
            var evaluator = new LessOrEqualsEvaluator("GuidProperty", "8DCE9920-E985-4B63-8ECE-A22160421FA3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("GuidProperty", "{8DCE9920-E985-4B63-8ECE-A22160421FA3}");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("GuidProperty", "67FED6E7-23C5-45DB-96DE-F763E169C922");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("GuidProperty", "not a GUID value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        // Ensure that expressions using only "time" portion of the timestamp are compared using today's date as a default.            
        [Fact]
        public void PartialTimestampTest()
        {
            DateTime timestamp = DateTime.Now;
            if (timestamp.Hour == 0 && timestamp.Minute <= 5)
            {
                // The range of timestamps checked is about 4 minutes, so the test will fail if it is shortly past midnight. 
                // In that case we simply do not perform the test, to avoid false negatives.

                // The test is using current machine time and cannot produce a result within 5 minutes past midnight local time;
                return;
            }

            string timestampString = timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var evaluator = new LessOrEqualsEvaluator("TwoMinutesAgoProperty", timestampString);
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            timestamp = DateTime.Now - TimeSpan.FromMinutes(4);
            timestampString = timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            evaluator = new LessOrEqualsEvaluator("TwoMinutesAgoProperty", timestampString);
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void EnumPropertyEquality()
        {
            var evaluator = new LessOrEqualsEvaluator("EnumProperty", "Warning");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("EnumProperty", "3");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("EnumProperty", "Informational");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("EnumProperty", "4");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("EnumProperty", "Error");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessOrEqualsEvaluator("EnumProperty", "2");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

        }
    }
}
