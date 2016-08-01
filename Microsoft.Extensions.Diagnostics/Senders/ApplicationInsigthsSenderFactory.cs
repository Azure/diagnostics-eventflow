// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public class ApplicationInsigthsSenderFactory
    {
        public static ApplicationInsightsSender CreateSender(IConfigurationRoot configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            IConfiguration aiConfiguration = configuration.GetSection("ApplicationInsightsSender");
            if (aiConfiguration == null)
            {
                healthReporter.ReportProblem("ApplicationInsightsSender configuration is missing");
                return null;
            }

            return new ApplicationInsightsSender(aiConfiguration, healthReporter);
        }
    }
}
