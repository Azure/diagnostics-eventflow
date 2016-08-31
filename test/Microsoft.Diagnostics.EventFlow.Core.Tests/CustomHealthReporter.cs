// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Tests
{
    public class CustomHealthReporter: IHealthReporter
    {
        public CustomHealthReporter(IConfiguration configuration)
        {

        }

        public void Dispose()
        {
        }

        public void ReportHealthy(string description = null, string context = null)
        {
            throw new NotImplementedException();
        }

        public void ReportProblem(string description, string context = null)
        {
            throw new NotImplementedException();
        }

        public void ReportWarning(string description, string context = null)
        {
            throw new NotImplementedException();
        }
    }
}
