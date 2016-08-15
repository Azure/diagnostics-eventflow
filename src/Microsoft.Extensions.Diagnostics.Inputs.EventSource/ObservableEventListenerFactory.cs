// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Configuration;
using System;
using Validation;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics
{
    public class ObservableEventListenerFactory: IPipelineItemFactory<ObservableEventListener>
    {
        public ObservableEventListener CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            IConfiguration sourcesConfiguration = configuration.GetSection("sources");
            if (sourcesConfiguration == null)
            {
                healthReporter.ReportProblem($"{nameof(ObservableEventListenerFactory)}: required configuration section 'sources' is missing");
                return null;
            }
            var eventSourceConfigurations = new List<EventSourceConfiguration>();
            try
            {
                sourcesConfiguration.Bind(eventSourceConfigurations);
            }
            catch
            {
                healthReporter.ReportProblem($"{nameof(ObservableEventListenerFactory)}: configuration is invalid");
                return null;
            }

            return new ObservableEventListener(eventSourceConfigurations, healthReporter);
        }
    }
}
