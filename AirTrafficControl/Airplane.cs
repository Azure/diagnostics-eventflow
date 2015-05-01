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
        public Task<AirplaneActorState> GetState()
        {
            return Task.FromResult(this.State);
        }

        public Task ReceiveInstruction(AtcInstruction instruction)
        {
            this.State.Instruction = instruction;
            ActorEventSource.Current.ActorMessage(this, "{0} received ATC instruction '{1}'", this.Id.ToString(), instruction.ToString());
            return Task.FromResult(true);
        }

        public Task TimePassed()
        {
            AirplaneState newState = this.State.AirplaneState.ComputeNextState(this.State.FlightPlan, this.State.Instruction);
            this.State.AirplaneState = newState;
            ActorEventSource.Current.ActorMessage(this, "New state for {0} is {1}", this.Id.ToString(), newState);
            return Task.FromResult(true);
        }
    }
}
