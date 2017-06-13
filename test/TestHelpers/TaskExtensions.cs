// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventFlow.TestHelpers
{
    public static class TaskExtensions
    {
        public static void Forget(this Task task) { }

        // TaskStatus.WaitingForActiviation is used for tasks that depend on other tasks, such as ones created with .ContinueWith()
        public static bool IsNotStarted(this Task task) => task.Status == TaskStatus.Created || task.Status == TaskStatus.WaitingForActivation;
    }
}
