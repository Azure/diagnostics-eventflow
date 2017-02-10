// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Xunit;

using Microsoft.Diagnostics.EventFlow.Utilities.Etw;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{

    public class ActivityPathDecoderTests
    {
        [Fact]
        public void ActivityPathDecoderDecodesHierarchicalActivityId()
        {
            Guid activityId = new Guid("000000110000000000000000be999d59");
            string activityPath = ActivityPathDecoder.GetActivityPathString(activityId);
            Assert.Equal("//1/1/", activityPath);
        }

        [Fact]
        public void ActivityPathDecoderHandlesNonhierarchicalActivityIds()
        {
            string guidString = "bf0209f9-bf5e-415e-86ed-0e20b615b406";
            Guid activityId = new Guid(guidString);
            string activityPath = ActivityPathDecoder.GetActivityPathString(activityId);
            Assert.Equal(guidString, activityPath);
        }

        [Fact]
        public void ActivityPathDecoderHandlesEmptyActivityId()
        {
            string activityPath = ActivityPathDecoder.GetActivityPathString(Guid.Empty);
            Assert.Equal(Guid.Empty.ToString(), activityPath);
        }
    }
}
