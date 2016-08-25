// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics
{
    public interface IPipelineItemFactory<out ItemType>
    {
        ItemType CreateItem(IConfiguration configuration, IHealthReporter healthReporter);
    }
}
