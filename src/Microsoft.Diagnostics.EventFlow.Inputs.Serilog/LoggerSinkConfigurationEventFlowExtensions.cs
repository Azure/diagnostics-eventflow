// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Serilog.Configuration;
using Serilog.Debugging;
using System.Linq;
using Validation;

namespace Serilog
{
    /// <summary>
    /// Extends <see cref="LoggerSinkConfiguration"/>
    /// </summary>
    public static class LoggerSinkConfigurationEventFlowExtensions
    {
        /// <summary>
        /// Publish Serilog events through a <see cref="DiagnosticPipeline"/>.
        /// </summary>
        /// <param name="loggerSinkConfiguration">The <c>WriteTo</c> object exposed by <see cref="LoggerConfiguration"/>.</param>
        /// <param name="diagnosticPipeline">A configured <see cref="DiagnosticPipeline"/>. At least one Serilog input
        /// must be configured for the pipeline.</param>
        /// <returns>A <see cref="LoggerConfiguration"/> allowing configuration to continue.</returns>
        /// <remarks>The method will select the first <see cref="SerilogInput"/> in the pipeline. If a specific input is desired instead,
        /// this can be passed to <see cref="LoggerSinkConfiguration.Sink(Core.ILogEventSink, Events.LogEventLevel, Core.LoggingLevelSwitch)"/>
        /// instead.</remarks>
        public static LoggerConfiguration EventFlow(this LoggerSinkConfiguration loggerSinkConfiguration, DiagnosticPipeline diagnosticPipeline)
        {
            Requires.NotNull(loggerSinkConfiguration, nameof(loggerSinkConfiguration));
            Requires.NotNull(diagnosticPipeline, nameof(diagnosticPipeline));

            var input = diagnosticPipeline.Inputs.OfType<SerilogInput>().FirstOrDefault();

            if (input == null)
            {
                SelfLog.WriteLine("{0}: A Serilog input has not been added to the diagnostic pipeline; events will not be published to EventFlow", nameof(LoggerSinkConfigurationEventFlowExtensions));
                return loggerSinkConfiguration.Sink(new LoggerConfiguration().CreateLogger()); // A "null" sink.
            }

            return loggerSinkConfiguration.Sink(input);
        }
    }
}
