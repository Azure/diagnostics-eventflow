// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Linq;
using Microsoft.Diagnostics.EventFlow.Inputs.ILogger.Logger;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public static class LoggerFactoryExtenstions
    {
        public static ILoggerFactory AddEventFlow(this ILoggerFactory factory, DiagnosticPipeline pipeline)
        {
            Validation.Requires.NotNull(factory, nameof(factory));
            Validation.Requires.NotNull(pipeline, nameof(pipeline));

            var loggerInputs = pipeline.Inputs?.Where(i => i is LoggerInput).ToArray();
            Validation.Requires.NotNullEmptyOrNullElements(loggerInputs, "LoggerInput");

            factory.AddProvider(new EventFlowLoggerProvider(loggerInputs.First() as LoggerInput));
            return factory;
        }
    }
}
