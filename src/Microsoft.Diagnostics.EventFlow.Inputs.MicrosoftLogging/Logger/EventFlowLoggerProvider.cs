// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class EventFlowLoggerProvider : ILoggerProvider
    {
        private readonly LoggerInput loggerInput;
        private readonly IHealthReporter reporter;

        public EventFlowLoggerProvider(LoggerInput loggerInput, IHealthReporter reporter)
        {
            Validation.Requires.NotNull(loggerInput, nameof(loggerInput));
            Validation.Requires.NotNull(reporter, nameof(reporter));

            this.loggerInput = loggerInput;
            this.reporter = reporter;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new EventFlowLogger(categoryName, loggerInput, reporter);
        }

        public void Dispose() { }
    }
}
