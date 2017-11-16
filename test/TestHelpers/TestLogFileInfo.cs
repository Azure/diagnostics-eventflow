// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Microsoft.Diagnostics.EventFlow.TestHelpers
{
    public class TestLogFileInfo : ILogFileInfo
    {
        public TestLogFileInfo(DateTime createTimeUtc = default(DateTime))
        {
            this.CreationTimeUtc = createTimeUtc;
        }

        public DateTime CreationTimeUtc { get; set; }

        public void Delete()
        {

        }
    }
}
