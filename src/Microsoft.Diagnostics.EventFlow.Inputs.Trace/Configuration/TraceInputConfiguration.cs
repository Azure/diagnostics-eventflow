// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Diagnostics;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    public class TraceInputConfiguration: ItemConfiguration
    {
        public SourceLevels TraceLevel { get; set; }

        public TraceInputConfiguration()
        {
            TraceLevel = SourceLevels.Error;
        }
    }
}
