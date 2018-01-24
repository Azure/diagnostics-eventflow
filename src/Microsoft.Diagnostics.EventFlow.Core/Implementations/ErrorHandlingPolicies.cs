// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Utilities
{
    public class ErrorHandlingPolicies
    {
        public static void HandleOutputTaskError(Exception e, Action handler)
        {
            if (e is TaskCanceledException)
            {
                // This exception is expected when the pipeline is shutting down--disregard.
            }
            else
            {
                handler();
            }
        }
    }
}
