using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Interfaces
{
    [DataContract]
    public class AirplaneActorState
    {
        [DataMember]
        public AirplaneState AirplaneState { get; set; }

        [DataMember]
        public FlightPlan FlightPlan { get; set; }

        [DataMember]
        public int DepartureTime { get; set; }

        [DataMember]
        public AtcInstruction Instruction { get; set; }
    }
}
