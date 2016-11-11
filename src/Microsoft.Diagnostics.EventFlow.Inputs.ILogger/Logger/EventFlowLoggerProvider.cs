// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.EventFlow.Inputs.ILogger.Logger
{
    public class EventFlowLoggerProvider : ILoggerProvider
    {
        private readonly LoggerInput loggerInput;

        public EventFlowLoggerProvider(LoggerInput loggerInput)
        {
            Validation.Requires.NotNull(loggerInput, nameof(loggerInput));
            this.loggerInput = loggerInput;
        }

        public Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            return new EventFlowLogger(categoryName, loggerInput);
        }

        public void Dispose() { }
    }
}
