using AirTrafficControl.Interfaces;
using Microsoft;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Validation;

namespace AirTrafficControl
{
    [StatePersistence(StatePersistence.Persisted)]
    public class Airplane : Actor, IAirplane
    {
        public Task<AirplaneActorState> GetStateAsync()
        {
            return this.StateManager.GetStateAsync<AirplaneActorState>(nameof(AirplaneActorState));
        }

        public async Task ReceiveInstructionAsync(AtcInstruction instruction)
        {
            Requires.NotNull(instruction, "instruction");

            var state = await GetStateAsync();
            if (state.AirplaneState is UnknownLocationState)
            {
                throw new InvalidOperationException("Cannot receive ATC instruction if the airplane location is unknown. The airplane needs to start the flight first.");
            }

            state.Instruction = instruction;
            await SetState(state);
            ActorEventSource.Current.ActorMessage(this, "{0}: Received ATC instruction '{1}'", this.Id.ToString(), instruction.ToString());
        }

        public async Task TimePassedAsync(int currentTime)
        {
            var actorState = await GetStateAsync();
            if (actorState.AirplaneState is UnknownLocationState)
            {
                return;
            }

            AirplaneState newAirplaneState = actorState.AirplaneState.ComputeNextState(actorState.FlightPlan, actorState.Instruction);
            actorState.AirplaneState = newAirplaneState;
            if (newAirplaneState is DepartingState)
            {
                actorState.DepartureTime = currentTime;
            }
            await SetState(actorState);
            ActorEventSource.Current.ActorMessage(this, "{0}: Now {1} Time is {2}", this.Id.ToString(), newAirplaneState, currentTime);
        }

        public async Task StartFlightAsync(FlightPlan flightPlan)
        {
            Requires.NotNull(flightPlan, "flightPlan");
            Requires.Argument(flightPlan.AirplaneID == this.Id.ToString(), "flightPlan", "The passed flight plan is for a different airplane");

            var actorState = await GetStateAsync();
            actorState.AirplaneState = new TaxiingState(flightPlan.DeparturePoint);
            actorState.FlightPlan = flightPlan;
            await SetState(actorState);
        }

        protected override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            var actorState = new AirplaneActorState();
            actorState.AirplaneState = UnknownLocationState.Instance;
            await this.StateManager.TryAddStateAsync(nameof(AirplaneActorState), actorState);
        }

        private Task SetState(AirplaneActorState state)
        {
            Requires.NotNull(state, nameof(state));
            return this.StateManager.SetStateAsync<AirplaneActorState>(nameof(AirplaneActorState), state);
        }
    }
}
