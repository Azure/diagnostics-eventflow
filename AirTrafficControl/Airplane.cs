using AirTrafficControl.Interfaces;
using Microsoft;
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
            Requires.NotNull(instruction, "instruction");

            this.State.Instruction = instruction;
            ActorEventSource.Current.ActorMessage(this, "{0} received ATC instruction '{1}'", this.Id.ToString(), instruction.ToString());
            return Task.FromResult(true);
        }

        public Task TimePassed(int currentTime)
        {
            AirplaneState newState = this.State.AirplaneState.ComputeNextState(this.State.FlightPlan, this.State.Instruction);
            this.State.AirplaneState = newState;
            if (newState is DepartingState)
            {
                this.State.DepartureTime = currentTime;
            }
            ActorEventSource.Current.ActorMessage(this, "Airplane {0} is now {1}", this.Id.ToString(), newState);
            return Task.FromResult(true);
        }

        public Task StartFlight(FlightPlan flightPlan)
        {
            Requires.NotNull(flightPlan, "flightPlan");

            this.State.AirplaneState = new TaxiingState(flightPlan.DeparturePoint);
            this.State.FlightPlan = flightPlan;
            return Task.FromResult(true);
        }
    }
}
