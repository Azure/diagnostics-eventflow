// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.Pipeline
{
    public interface IHealthReporter
    {
        void ReportHealthy();
        void ReportProblem(string problemDescription);
    }
}