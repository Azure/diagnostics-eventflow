// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Validation;

namespace Microsoft.Diagnostics.Pipeline
{
    public class DiagnosticsPipeline
    {
        public DiagnosticsPipeline()
        {
        }

        protected virtual void Initialize(
            IConfiguration configuration,
            IHealthReporter healthReporter,
            IReadOnlyCollection<IObservable<EventData>> sources,
            IEnumerable<Action<EventData>> decorators,
            IEnumerable<IObserver<EventData>> senders)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            // Create EtwListener, passing the configuration (EtwListener just reads the EtwProviders property).
            // var etwProvidersString = configuration["EtwProviders"];
            // var etwProviders = JsonConvert.DeserializeObject<List<EtwProviderConfiguration>>(etwProvidersString);

            // Create BufferingEventDecorator + all decorators

            // Create
        }
    }
}