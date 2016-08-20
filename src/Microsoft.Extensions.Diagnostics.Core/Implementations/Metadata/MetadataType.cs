// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public static class MetadataType
    {
        public static readonly string Metric = "metric";
        public static readonly string Request = "request";

        // TODO: add some validation for well-known metadata

        // CONSIDER: strongly-typed classes that define properties for well-known metadata
    }
}
