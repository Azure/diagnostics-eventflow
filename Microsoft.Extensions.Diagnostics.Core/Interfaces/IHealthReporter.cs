// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Microsoft.Extensions.Diagnostics
{
    public interface IHealthReporter : IDisposable
    {
        void ReportHealthy();
        void ReportProblem(string problemDescription, string category = null);
        void ReportMessage(string description, string category = null);
        void ReportWarning(string description, string category = null);
    }
}