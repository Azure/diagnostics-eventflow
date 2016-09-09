// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class DummyOutput : IOutput
    {
        public Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class DummyOutputFactory : IPipelineItemFactory<DummyOutput>
    {
        public DummyOutput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            return new DummyOutput();
        }
    }
}
