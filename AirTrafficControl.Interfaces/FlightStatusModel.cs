using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AirTrafficControl.Interfaces
{
    [DataContract]
    public class FlightStatusModel
    {
        // Parameterless constructor for deserialization
        public FlightStatusModel() { }

        [DataMember]
        public IEnumerable<AirplaneStateDto> AirplaneStates { get; set; }

        [DataMember]
        public double EstimatedNextStatusUpdateDelayMsec { get; set; }
    }
}
