// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class EventSourceInput_ConstructorShould
    {
        [Fact(DisplayName = "EventSourceInput ctor should create instance")]
        public void CreateInstance()
        {
            // Setup
            IConfiguration dummyConfigure = new DummyConfiguration();
            IHealthReporter dummyHealthReporter = new DummyHealthReporter();

            try
            {
                // Execute
                ObservableEventListener target = new ObservableEventListener(
                    dummyConfigure,
                    dummyHealthReporter);

                // Verify
                Assert.NotNull(target);
            }
            finally
            {
                // Teardown
                dummyConfigure = null;
                dummyHealthReporter = null;
            }
        }
    }
}
