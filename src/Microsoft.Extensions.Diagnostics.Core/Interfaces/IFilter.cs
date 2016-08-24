// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics
{
    public interface IFilter
    {
        // Returns false if the event should be discarded.
        bool Filter(EventData eventData);
    }
}
