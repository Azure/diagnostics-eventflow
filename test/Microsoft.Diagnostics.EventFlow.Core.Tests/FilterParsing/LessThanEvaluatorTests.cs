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
    public class LessThanEvaluatorTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void IdPropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("EventId", "1234");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("EventId", "-34");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("EventId", "1235");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("EventId", "notIntegerValue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MessagePropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("Message", "Test event with many properties of different types");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("Message", "aaa");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("Message", "zzz");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void TimestampPropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("Timestamp", "2015-05-29T10:45:00.537Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same timestamp
            evaluator = new LessThanEvaluator("Timestamp", "2015-05-29T10:45:00.537+00:00");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("Timestamp", "2015-05-29T10:50:00Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("Timestamp", "2015-05-29T10:45:00Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("Timestamp", "not a timestamp value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MissingPropertyLessThan()
        {
            var evaluator = new LessThanEvaluator("InvalidPropertyName", "somevalue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void StringPropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("StringProperty", "Ala ma kota");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Comparison should be case-insensitive
            evaluator = new LessThanEvaluator("StringProperty", "Ala MA kota");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Greater value
            evaluator = new LessThanEvaluator("StringProperty", "Kota ma Ala");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("StringProperty", "AAla");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void IntPropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("IntProperty", "-65000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("IntProperty", "65000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("IntProperty", "-65001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("IntProperty", "not an int value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void LongPropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("LongProperty", "-5000000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("LongProperty", "5000000000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("LongProperty", "-5000000001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessThanEvaluator("LongProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("LongProperty", "not a long value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ShortPropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("ShortProperty", "-18000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("ShortProperty", "18000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("ShortProperty", "-18001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessThanEvaluator("ShortProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for short values
            evaluator = new LessThanEvaluator("ShortProperty", "70000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void SbytePropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("SbyteProperty", "-20");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("SbyteProperty", "20");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("SbyteProperty", "-21");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessThanEvaluator("SbyteProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new LessThanEvaluator("SbyteProperty", "234");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UintPropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("UintProperty", "80000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("UintProperty", "80001");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("UintProperty", "70500");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessThanEvaluator("UintProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for uint
            evaluator = new LessThanEvaluator("UintProperty", "-80000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UlongPropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("UlongProperty", "5100000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("UlongProperty", "5100000001");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("UlongProperty", "5000000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessThanEvaluator("UlongProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new LessThanEvaluator("UlongProperty", "-5100000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UshortPropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("UshortProperty", "18200");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("UshortProperty", "18201");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("UshortProperty", "18199");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessThanEvaluator("UshortProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new LessThanEvaluator("UshortProperty", "-18200");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BytePropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("ByteProperty", "7");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("ByteProperty", "8");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("ByteProperty", "3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessThanEvaluator("ByteProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for byte
            evaluator = new LessThanEvaluator("ByteProperty", "-3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void FloatPropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("FloatProperty", "-7.4");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different representation of the same number
            evaluator = new LessThanEvaluator("FloatProperty", "-74E-1");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("FloatProperty", "74E-1");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("FloatProperty", "-7.5");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessThanEvaluator("FloatProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new LessThanEvaluator("FloatProperty", "-2.347E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DoublePropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("DoubleProperty", "-2.347E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different representation of the same number
            evaluator = new LessThanEvaluator("DoubleProperty", "-234.7E41");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("DoubleProperty", "2.347E43");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("DoubleProperty", "-2.3471E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new LessThanEvaluator("DoubleProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("DoubleProperty", "not a double value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimePropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("DateTimeProperty", "2015-03-30T09:15:00");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same value
            evaluator = new LessThanEvaluator("DateTimeProperty", "2015-03-30 09:15:00");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("DateTimeProperty", "2015-03-30T09:15:01");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("DateTimeProperty", "2015-03-30T09:14:59");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("DateTimeProperty", "not a DateTime value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimeOffsetPropertyLessThan()
        {
            // Same value
            var evaluator = new LessThanEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same value
            evaluator = new LessThanEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485+00:00");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value in different time zone
            evaluator = new LessThanEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485+00:01");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new LessThanEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new LessThanEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:18Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("DateTimeOffsetProperty", "not a DateTimeOffset value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BoolPropertyLessThan()
        {
            // Bool properties do not work with less-than operator, so no matter what RHS value we use the evaluation should always be false.
            var evaluator = new LessThanEvaluator("BoolProperty", "true");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("BoolProperty", "false");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("BoolProperty", "not a bool value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void GuidPropertyLessThan()
        {
            // GUID properties do not work with less-than operator, so no matter what RHS value we use the evaluation should always be false.
            var evaluator = new LessThanEvaluator("GuidProperty", "8DCE9920-E985-4B63-8ECE-A22160421FA3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("GuidProperty", "{8DCE9920-E985-4B63-8ECE-A22160421FA3}");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("GuidProperty", "67FED6E7-23C5-45DB-96DE-F763E169C922");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new LessThanEvaluator("GuidProperty", "not a GUID value");
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
            var evaluator = new LessThanEvaluator("TwoMinutesAgoProperty", timestampString);
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            timestamp = DateTime.Now - TimeSpan.FromMinutes(4);
            timestampString = timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            evaluator = new LessThanEvaluator("TwoMinutesAgoProperty", timestampString);
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }
    }
}
