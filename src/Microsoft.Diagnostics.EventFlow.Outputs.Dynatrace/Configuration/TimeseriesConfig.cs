using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model
{
    public class TimeseriesConfig : TimeseriesRegistrationMessage
    {
        public string timeseriesId { get; set; }
        public TimeseriesConfig ()
        {
        }
        public TimeseriesConfig (TimeseriesConfig  def)
        {
            this.timeseriesId = def.timeseriesId;
            this.displayName = def.displayName;
            this.unit = def.unit;
            this.dimensions = def.dimensions.Clone() as string[];
            this.types = def.types.Clone() as string[];
        }

    }
}
