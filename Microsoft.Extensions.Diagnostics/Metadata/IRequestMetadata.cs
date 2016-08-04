// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public interface IRequestMetadata
    {
        string RequestNameProperty { get; set; }
        string DurationProperty { get; set; }
        string IsSuccessProperty { get; set; }
    }
}
