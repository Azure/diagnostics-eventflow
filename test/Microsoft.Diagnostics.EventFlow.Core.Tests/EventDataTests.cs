// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Reflection;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class EventDataTests
    {
        [Fact]
        public void GetPropertyValueCoverAllCommonProperty()
        {
            var data = new EventData();
            var props = data.GetType().GetProperties();
            object dummy;

            foreach(var prop in props)
            {
                if (prop.Name != "Payload")
                {
                    Assert.True(data.TryGetPropertyValue(prop.Name, out dummy));
                }
            }
        }

        [Fact]
        public void GetPropertyValueGetValueFromPayload()
        {
            var data = new EventData();
            data.Payload.Add("prop", "value");

            object value;
            Assert.True(data.TryGetPropertyValue("prop", out value));
            Assert.Equal("value", value);
        }
    }
}
