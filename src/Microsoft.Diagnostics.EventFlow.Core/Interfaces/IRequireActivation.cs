// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow
{
    public interface IRequireActivation
    {
        /// <summary>
        /// Invoke the activation.
        /// </summary>
        void Activate();
    }
}
