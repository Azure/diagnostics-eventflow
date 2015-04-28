using AirTrafficControl.Interfaces;

using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl
{
    public class Airplane : Actor<AirplaneActorState>, IAirplane
    {
        public Task ReceiveInstruction(AtcInstruction instruction)
        {
            this.State.Instruction = instruction;
            ActorEventSource.Current.ActorMessage(this, "Received ATC instruction '{0}'", instruction.ToString());
            return Task.FromResult(true);
        }

        public Task TimePassed()
        {
            // TODO: implement
            return Task.FromResult(true);
        }
    }

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
