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
    public class GreaterThanEvaluatorTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void IdPropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("EventId", "1234");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("EventId", "-34");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("EventId", "1235");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("EventId", "notIntegerValue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MessagePropertyGreaterThan()
        {
            var evaluator = new GreaterThanEvaluator("Message", "Test event with many properties of different types");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("Message", "aaa");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("Message", "zzz");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void TimestampPropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("Timestamp", "2015-05-29T10:45:00.537Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same timestamp
            evaluator = new GreaterThanEvaluator("Timestamp", "2015-05-29T10:45:00.537+00:00");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("Timestamp", "2015-05-29T10:50:00Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("Timestamp", "2015-05-29T10:45:00Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("Timestamp", "not a timestamp value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MissingPropertyGreaterThan()
        {
            var evaluator = new GreaterThanEvaluator("InvalidPropertyName", "somevalue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void StringPropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("StringProperty", "Ala ma kota");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Comparison should be case-insensitive
            evaluator = new GreaterThanEvaluator("StringProperty", "Ala MA kota");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Greater value
            evaluator = new GreaterThanEvaluator("StringProperty", "Kota ma Ala");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("StringProperty", "AAla");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void IntPropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("IntProperty", "-65000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("IntProperty", "65000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("IntProperty", "-65001");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("IntProperty", "not an int value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void LongPropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("LongProperty", "-5000000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("LongProperty", "5000000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("LongProperty", "-5000000001");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterThanEvaluator("LongProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("LongProperty", "not a long value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ShortPropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("ShortProperty", "-18000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("ShortProperty", "18000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("ShortProperty", "-18001");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterThanEvaluator("ShortProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for short values
            evaluator = new GreaterThanEvaluator("ShortProperty", "70000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void SbytePropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("SbyteProperty", "-20");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("SbyteProperty", "20");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("SbyteProperty", "-21");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterThanEvaluator("SbyteProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new GreaterThanEvaluator("SbyteProperty", "234");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UintPropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("UintProperty", "80000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("UintProperty", "80001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("UintProperty", "70500");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterThanEvaluator("UintProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for uint
            evaluator = new GreaterThanEvaluator("UintProperty", "-80000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UlongPropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("UlongProperty", "5100000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("UlongProperty", "5100000001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("UlongProperty", "5000000000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterThanEvaluator("UlongProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new GreaterThanEvaluator("UlongProperty", "-5100000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UshortPropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("UshortProperty", "18200");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("UshortProperty", "18201");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("UshortProperty", "18199");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterThanEvaluator("UshortProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new GreaterThanEvaluator("UshortProperty", "-18200");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BytePropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("ByteProperty", "7");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("ByteProperty", "8");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("ByteProperty", "3");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterThanEvaluator("ByteProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for byte
            evaluator = new GreaterThanEvaluator("ByteProperty", "-3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void FloatPropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("FloatProperty", "-7.4");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different representation of the same number
            evaluator = new GreaterThanEvaluator("FloatProperty", "-74E-1");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("FloatProperty", "74E-1");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("FloatProperty", "-7.5");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterThanEvaluator("FloatProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new GreaterThanEvaluator("FloatProperty", "-2.347E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DoublePropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("DoubleProperty", "-2.347E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different representation of the same number
            evaluator = new GreaterThanEvaluator("DoubleProperty", "-234.7E41");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("DoubleProperty", "2.347E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("DoubleProperty", "-2.3471E43");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterThanEvaluator("DoubleProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("DoubleProperty", "not a double value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimePropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("DateTimeProperty", "2015-03-30T09:15:00");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same value
            evaluator = new GreaterThanEvaluator("DateTimeProperty", "2015-03-30 09:15:00");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("DateTimeProperty", "2015-03-30T09:15:01");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("DateTimeProperty", "2015-03-30T09:14:59");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("DateTimeProperty", "not a DateTime value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimeOffsetPropertyGreaterThan()
        {
            // Same value
            var evaluator = new GreaterThanEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same value
            evaluator = new GreaterThanEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485+00:00");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value in different time zone
            evaluator = new GreaterThanEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485+00:01");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterThanEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterThanEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:18Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("DateTimeOffsetProperty", "not a DateTimeOffset value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BoolPropertyGreaterThan()
        {
            // Bool properties do not work with greater-than operator, so no matter what RHS value we use the evaluation should always be false.
            var evaluator = new GreaterThanEvaluator("BoolProperty", "true");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("BoolProperty", "false");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("BoolProperty", "not a bool value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void GuidPropertyGreaterThan()
        {
            // GUID properties do not work with greater-than operator, so no matter what RHS value we use the evaluation should always be false.
            var evaluator = new GreaterThanEvaluator("GuidProperty", "8DCE9920-E985-4B63-8ECE-A22160421FA3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("GuidProperty", "{8DCE9920-E985-4B63-8ECE-A22160421FA3}");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("GuidProperty", "67FED6E7-23C5-45DB-96DE-F763E169C922");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterThanEvaluator("GuidProperty", "not a GUID value");
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
            var evaluator = new GreaterThanEvaluator("TwoMinutesAgoProperty", timestampString);
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            timestamp = DateTime.Now - TimeSpan.FromMinutes(4);
            timestampString = timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            evaluator = new GreaterThanEvaluator("TwoMinutesAgoProperty", timestampString);
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }
    }
}
