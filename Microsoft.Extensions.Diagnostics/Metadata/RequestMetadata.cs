// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public class RequestMetadata: EventMetadata
    {
        public string RequestNameProperty { get; set; }
        public string DurationProperty { get; set; }
        public string IsSuccessProperty { get; set; }
    }
}
