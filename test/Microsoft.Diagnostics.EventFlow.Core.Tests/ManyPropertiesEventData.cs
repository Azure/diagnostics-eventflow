// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Diagnostics.EventFlow.TestHelpers;
using System;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    internal class ManyPropertiesEventData: EventData
    {
        public static readonly string BoolProperty = nameof(BoolProperty);
        public static readonly string DateTimeProperty = nameof(DateTimeProperty);
        public static readonly string DateTimeOffsetProperty = nameof(DateTimeOffsetProperty);
        public static readonly string GuidProperty = nameof(GuidProperty);
        public static readonly string TimeSpanProperty = nameof(TimeSpanProperty);
        public static readonly string FloatProperty = nameof(FloatProperty);
        public static readonly string DoubleProperty = nameof(DoubleProperty);
        public static readonly string ByteProperty = nameof(ByteProperty);
        public static readonly string ShortProperty = nameof(ShortProperty);
        public static readonly string UShortProperty = nameof(UShortProperty);
        public static readonly string IntProperty = nameof(IntProperty);
        public static readonly string UIntProperty = nameof(UIntProperty);
        public static readonly string LongProperty = nameof(LongProperty);
        public static readonly string ULongProperty = nameof(ULongProperty);
        public static readonly string StringProperty = nameof(StringProperty);

        public static readonly bool ExpectedBoolValue = true;
        public static readonly DateTime ExpectedDateTimeValue = new DateTime(2020, 12, 23, 14, 20, 16, DateTimeKind.Utc);
        public static readonly DateTimeOffset ExpectedDateTimeOffsetValue = new DateTimeOffset(2020, 12, 23, 14, 45, 37, TimeSpan.FromHours(-8.0)); // Pacific time
        public static readonly Guid ExpectedGuidValue = Guid.Parse("e3cfc82f-d29f-4dbc-85f0-d43084325a7b");
        public static readonly TimeSpan ExpectedTimeSpanValue = TimeSpan.FromMinutes(22);
        public static readonly float ExpectedFloatValue = (float)Math.PI;
        public static readonly double ExpectedDoubleValue = Math.E;
        public static readonly byte ExpectedByteValue = (byte)32;
        public static readonly short ExpectedShortValue = (short)-12000;
        public static readonly ushort ExpectedUShortValue = (ushort)20000;
        public static readonly int ExpectedIntValue = (int)-2022;
        public static readonly uint ExpectedUIntValue = (uint)100000;
        public static readonly long ExpectedLongValue = (long)-12345678;
        public static readonly ulong ExpectedULongValue = (ulong)999999;
        public static readonly string ExpectedStringValue = "Keyser Söze";

        public ManyPropertiesEventData()
        {
            this.Payload[BoolProperty] = ExpectedBoolValue;
            this.Payload[DateTimeProperty] = ExpectedDateTimeValue;
            this.Payload[DateTimeOffsetProperty] = ExpectedDateTimeOffsetValue; 
            this.Payload[GuidProperty] = ExpectedGuidValue;
            this.Payload[TimeSpanProperty] = ExpectedTimeSpanValue;
            this.Payload[FloatProperty] = ExpectedFloatValue;
            this.Payload[DoubleProperty] = ExpectedDoubleValue;
            this.Payload[ByteProperty] = ExpectedByteValue;
            this.Payload[ShortProperty] = ExpectedShortValue;
            this.Payload[UShortProperty] = ExpectedUShortValue;
            this.Payload[IntProperty] = ExpectedIntValue;
            this.Payload[UIntProperty] = ExpectedUIntValue;
            this.Payload[LongProperty] = ExpectedLongValue;
            this.Payload[ULongProperty] = ExpectedULongValue;
            this.Payload[StringProperty] = ExpectedStringValue;
        }
    }
}
