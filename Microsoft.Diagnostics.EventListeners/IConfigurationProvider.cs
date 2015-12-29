// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventListeners
{
    public interface IConfigurationProvider
    {
        bool HasConfiguration { get; }

        string GetValue(string name);
    }
}