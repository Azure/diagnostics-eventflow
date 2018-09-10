// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Linq;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Validation;

namespace NLog
{
    public static class DiagnosticPipelineExtensions
    {
        public static NLogInput ConfigureNLogInput(this DiagnosticPipeline diagnosticPipeline, LogLevel minLogLevel = null, string loggerNamePattern = "*", Config.LoggingConfiguration loggingConfig = null)
        {
            Requires.NotNull(diagnosticPipeline, nameof(diagnosticPipeline));

            var input = diagnosticPipeline.Inputs.OfType<NLogInput>().FirstOrDefault();
            if (input == null)
                return null;

            if (minLogLevel != null)
            {
                var config = loggingConfig ?? LogManager.Configuration ?? new Config.LoggingConfiguration();
                config.AddRule(minLogLevel, LogLevel.Fatal, input, loggerNamePattern);
                if (loggingConfig == null)
                {
                    if (LogManager.Configuration == null)
                        LogManager.Configuration = config;
                    else
                        LogManager.ReconfigExistingLoggers();
                }
            }

            return input;
        }
    }
}
