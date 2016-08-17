// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class DummyHealthReporter : IHealthReporter
    {
        public void Dispose()
        {
        }

        public void ReportHealthy()
        {
        }

        public void ReportMessage(string description, string category = null)
        {
        }

        public void ReportProblem(string problemDescription, string category = null)
        {
        }

        public void ReportWarning(string description, string category = null)
        {
        }
    }
}
