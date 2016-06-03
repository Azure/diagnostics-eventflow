using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric.Actors.Runtime;
using System;
using System.Threading.Tasks;
using Validation;
using System.Net;

namespace AirTrafficControl
{
    [StatePersistence(StatePersistence.Persisted)]
    public class Airplane : Actor, IAirplane
    {
        public Task<AirplaneActorState> GetStateAsync()
        {
            return PerformActorOperation(nameof(GetStateAsync), async () =>
             {
                 var state = await this.StateManager.GetStateAsync<AirplaneActorState>(nameof(AirplaneActorState));
                 return state;
             });            
        }

        public Task ReceiveInstructionAsync(AtcInstruction instruction)
        {
            Requires.NotNull(instruction, "instruction");

            return PerformActorOperation(nameof(ReceiveInstructionAsync), async () =>
            {
                var state = await GetStateAsync();
                if (state.AirplaneState is UnknownLocationState)
                {
                    throw new InvalidOperationException("Cannot receive ATC instruction if the airplane location is unknown. The airplane needs to start the flight first.");
                }

                state.Instruction = instruction;
                await SetState(state);

                ActorEventSource.Current.ActorMessage(this, "{0}: Received ATC instruction '{1}'", this.Id.ToString(), instruction.ToString());
                return true;
            });            
        }

        public Task TimePassedAsync(int currentTime)
        {
            return PerformActorOperation(nameof(TimePassedAsync), async () =>
            {
                var actorState = await GetStateAsync();
                if (actorState.AirplaneState is UnknownLocationState)
                {
                    return true;
                }

                AirplaneState newAirplaneState = actorState.AirplaneState.ComputeNextState(actorState.FlightPlan, actorState.Instruction);
                actorState.AirplaneState = newAirplaneState;
                if (newAirplaneState is DepartingState)
                {
                    actorState.DepartureTime = currentTime;
                }
                await SetState(actorState);

                ActorEventSource.Current.ActorMessage(this, "{0}: Now {1} Time is {2}", this.Id.ToString(), newAirplaneState, currentTime);
                return true; 
            });            
        }

        public Task StartFlightAsync(FlightPlan flightPlan)
        {
            Requires.NotNull(flightPlan, "flightPlan");
            Requires.Argument(flightPlan.AirplaneID == this.Id.ToString(), "flightPlan", "The passed flight plan is for a different airplane");

            return PerformActorOperation(nameof(StartFlightAsync), async () =>
            {
                var actorState = await GetStateAsync();
                actorState.AirplaneState = new TaxiingState(flightPlan.DeparturePoint);
                actorState.FlightPlan = flightPlan;
                await SetState(actorState);
                return true;
            });            
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

        private async Task<T> PerformActorOperation<T>(string methodName, Func<Task<T>> impl)
        {
            Requires.NotNullOrWhiteSpace(methodName, nameof(methodName));

            ActorEventSource.Current.ActorMethodStart(this, methodName);
            Exception unexpectedException = null;
            DateTime startTimeUtc = DateTime.UtcNow;
            try
            {
                T retval = await impl();
                return retval;
            }
            catch (Exception e)
            {
                unexpectedException = e;
                throw;
            }
            finally
            {
                HttpStatusCode statusCode = unexpectedException == null ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;

                ActorEventSource.Current.ActorMethodStop(
                    this,
                    methodName,
                    startTimeUtc,
                    DateTime.UtcNow - startTimeUtc,
                    statusCode,
                    unexpectedException?.ToString() ?? string.Empty);
            }
        }
    }
}
