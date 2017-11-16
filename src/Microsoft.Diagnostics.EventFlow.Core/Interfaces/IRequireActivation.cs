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
        /// <remarks>
        /// For some EventFlow items (health reporter, inputs etc.) it might be inconvenient to make them "start" during construction.
        /// These items can implement IRequireActivation and they will be "activated" after constructor completes. 
        /// DiagnosticPipelineFactory activates all items during pipeline construction automatically.
        /// If the pipeline is assembled imperatively, the programmer needs to specifically call Activate() on items that require it.
        /// </remarks>
        void Activate();
    }
}
