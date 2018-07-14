using Microsoft.Diagnostics.EventFlow;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using log4net.Core;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Log4net
{
    public class Log4NetFactory : IPipelineItemFactory<Log4NetInput>
    {
        public Log4NetInput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            return new Log4NetInput(configuration, healthReporter);
        }
    }
}
