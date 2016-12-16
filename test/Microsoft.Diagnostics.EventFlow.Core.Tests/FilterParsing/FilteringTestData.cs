// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.EventFlow;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests.FilterParsing
{
    internal static class FilteringTestData
    {
        public static readonly EventData ManyPropertiesEvent;

        static FilteringTestData()
        {
            var eventData = new EventData();

            eventData.Payload.Add("EventId", (int) 1234);
            eventData.Payload.Add("Message", "Test event with many properties of different types");
            eventData.Timestamp = DateTimeOffset.Parse("2015-05-29T10:45:00.537Z");

            eventData.Payload.Add("StringProperty", "Ala ma kota");

            eventData.Payload.Add("IntProperty", (int)-65000);
            eventData.Payload.Add("LongProperty", (long)-5000000000);
            eventData.Payload.Add("ShortProperty", (short)-18000);
            eventData.Payload.Add("SbyteProperty", (sbyte)-20);

            eventData.Payload.Add("UintProperty", (uint)80000);
            eventData.Payload.Add("UlongProperty", (ulong)5100000000);
            eventData.Payload.Add("UshortProperty", (ushort)18200);
            eventData.Payload.Add("ByteProperty", (byte)7);

            eventData.Payload.Add("FloatProperty", (float)-7.4);
            eventData.Payload.Add("DoubleProperty", (double)-2.347E43);

            eventData.Payload.Add("DateTimeProperty", DateTime.Parse("2015-03-30T09:15:00"));
            eventData.Payload.Add("DateTimeOffsetProperty", DateTimeOffset.Parse("2015-05-29T10:39:17.485Z"));

            eventData.Payload.Add("BoolProperty", true);

            eventData.Payload.Add("GuidProperty", Guid.Parse("8DCE9920-E985-4B63-8ECE-A22160421FA3"));

            eventData.Payload.Add("TwoMinutesAgoProperty", DateTime.Now - TimeSpan.FromMinutes(2));

            // Special properties for regex evaluator testing
            eventData.Payload.Add("AbaShortProperty", "abaaaa");
            eventData.Payload.Add("AbaLongProperty", "abaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            ManyPropertiesEvent = eventData;
        }
    }
}
