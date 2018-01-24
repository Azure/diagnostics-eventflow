// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventFlow.TestHelpers
{
    public static class TaskUtils
    {
        private static readonly TimeSpan JustABit = TimeSpan.FromMilliseconds(50);

        public static async Task<bool> PollWaitAsync(Func<bool> condition, TimeSpan timeout)
        {
            DateTime doNotExceedTime = DateTime.Now + timeout;
            while (!condition())
            {
                await Task.Delay(JustABit);
                if (DateTime.Now > doNotExceedTime)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
