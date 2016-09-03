// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Microsoft.Diagnostics.EventFlow
{
    public interface IHealthReporter : IDisposable
    {
        void ReportHealthy(string description = null, string context = null);
        void ReportProblem(string description, string context = null);
        void ReportWarning(string description, string context = null);
    }
}