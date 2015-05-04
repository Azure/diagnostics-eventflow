using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using Microsoft;

namespace AirTrafficControl
{
    public class AirTrafficControl : Actor<AirTrafficControlState>, IAirTrafficControl, IRemindable
    {
        private const string TimePassedReminder = "AirTrafficControl.TimePassedReminder";
        private delegate Task AirplaneController(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneState> projectedAirplaneStates);

        private readonly IDictionary<Type, AirplaneController> AirplaneControllers; 
    
        public AirTrafficControl(): base()
        {
            AirplaneControllers = new Dictionary<Type, AirplaneController>()
            {
                { typeof(TaxiingState), HandleAirplaneTaxiing },
                { typeof(DepartingState), HandleAirplaneDeparting },
                { typeof(HoldingState), HandleAirplaneHolding },
                { typeof(EnrouteState), HandleAirplaneEnroute },
                { typeof(ApproachState), HandleAirplaneApproaching },
                { typeof(LandedState), HandleAirplaneLanded }
            };
        }   

        public override Task OnActivateAsync()
        {
            if (this.State.FlyingAirplaneIDs == null)
            {
                this.State.FlyingAirplaneIDs = new List<string>();
            }

            IActorReminder reminder = null;
            try
            {
                reminder = this.GetReminder(TimePassedReminder);
            }
            catch { }
            if (reminder == null)
            {
                this.RegisterReminder(TimePassedReminder, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), ActorReminderAttributes.None);
                this.State.CurrentTime = 0;
            }

            return base.OnActivateAsync();
        }
        public Task<IEnumerable<string>> GetFlyingAirplaneIDs()
        {
            return Task.FromResult(this.State.FlyingAirplaneIDs.AsEnumerable());
        }

        public async Task StartNewFlight(FlightPlan flightPlan)
        {
            Requires.NotNull(flightPlan, "flightPlan");
            flightPlan.Validate();

            ActorId actorID = new ActorId(flightPlan.AirplaneID);
            IAirplane airplane = ActorProxy.Create<IAirplane>(actorID);
            await airplane.StartFlight(flightPlan);
            this.State.FlyingAirplaneIDs.Add(flightPlan.AirplaneID);
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            if (!TimePassedReminder.Equals(reminderName, StringComparison.Ordinal))
            {
                return;
            }

            var airplaneProxies = CreateAirplaneProxies();
            var currentAirplaneStatesByDepartureTime = (await Task.WhenAll(this.State.FlyingAirplaneIDs.Select(id => airplaneProxies[id].GetState())))
                                                       .OrderBy(state => (state.AirplaneState is TaxiingState) ? int.MaxValue : state.DepartureTime);
            var newAirplaneStates = new Dictionary<string, AirplaneState>();

            foreach(var currentState in currentAirplaneStatesByDepartureTime)
            {
                var controllerFunction = this.AirplaneControllers[currentState.GetType()];
                Assumes.NotNull(controllerFunction);

                controllerFunction(airplaneProxies[currentState.FlightPlan.AirplaneID], currentState, newAirplaneStates);
            }

            this.State.CurrentTime++;

            await Task.WhenAll(newAirplaneStates.Keys.Select(airplaneID => airplaneProxies[airplaneID].TimePassed(this.State.CurrentTime)));
        }

        private Dictionary<string, IAirplane> CreateAirplaneProxies()
        {
            var retval = new Dictionary<string, IAirplane>();
            foreach (var airplaneID in this.State.FlyingAirplaneIDs)
            {
                retval.Add(airplaneID, ActorProxy.Create<IAirplane>(new ActorId(airplaneID)));
            }
            return retval;
        }

        private Task HandleAirplaneLanded(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            // Just remove the airplane form the flying airplanes set
            this.State.FlyingAirplaneIDs.Remove(currentState.FlightPlan.AirplaneID);
            ActorEventSource.Current.ActorMessage(this, "Airplane {0} has landed and is no longer tracked by ATC", currentState.FlightPlan.AirplaneID);
            return Task.FromResult(true);
        }

        private Task HandleAirplaneApproaching(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            // We assume that every approach is successful, so just make a note that the airplane will be in the Landed state
            FlightPlan flightPlan = currentState.FlightPlan;
            Assumes.NotNull(flightPlan);
            projectedAirplaneStates[flightPlan.AirplaneID] = new LandedState(flightPlan.Destination);
            return Task.FromResult(true);
        }

        private async Task HandleAirplaneEnroute(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            EnrouteState enrouteState = (EnrouteState)currentState.AirplaneState;
            FlightPlan flightPlan = currentState.FlightPlan;
            Fix nextFix = enrouteState.Route.GetNextFix(enrouteState.To, flightPlan.Destination);

            if (nextFix == flightPlan.Destination)
            {
                // Any other airplanes cleared for landing at this airport?
                if (projectedAirplaneStates.OfType<ApproachState>().Any(state => state.Airport == nextFix))
                {
                    projectedAirplaneStates[flightPlan.AirplaneID] = new HoldingState(nextFix);
                    await airplaneProxy.ReceiveInstruction(new HoldInstruction(nextFix));
                    ActorEventSource.Current.ActorMessage(this, "Issued holding instruction for {0} at {1} because another airplane has been cleared for approach at the same airport", flightPlan.AirplaneID, nextFix.DisplayName);
                }

                // CONTINUE HERE
            }
        }

        private Task HandleAirplaneHolding(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            throw new NotImplementedException();
        }

        private Task HandleAirplaneDeparting(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            throw new NotImplementedException();
        }

        private Task HandleAirplaneTaxiing(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            throw new NotImplementedException();
        }
    }
}
