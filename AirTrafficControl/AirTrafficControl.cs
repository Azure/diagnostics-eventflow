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
        private delegate void AirplaneController(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneActorState> projectedAirplaneStates);

        private readonly IDictionary<Type, AirplaneController> AirplaneControllers = new Dictionary<Type, AirplaneController>()
        {
            { typeof(TaxiingState), HandleAirplaneTaxiing },
            { typeof(DepartingState), HandleAirplaneDeparting },
            { typeof(HoldingState), HandleAirplaneHolding },
            { typeof(EnrouteState), HandleAirplaneEnroute },
            { typeof(ApproachState), HandleAirplaneApproaching },
            { typeof(LandedState), HandleAirplaneLanded }
        };       

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
            var newAirplaneStates = new Dictionary<string, AirplaneActorState>();

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

        private static void HandleAirplaneLanded(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneActorState> projectedAirplaneStates)
        {
            throw new NotImplementedException();
        }

        private static void HandleAirplaneApproaching(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneActorState> projectedAirplaneStates)
        {
            throw new NotImplementedException();
        }

        private static void HandleAirplaneEnroute(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneActorState> projectedAirplaneStates)
        {
            throw new NotImplementedException();
        }

        private static void HandleAirplaneHolding(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneActorState> projectedAirplaneStates)
        {
            throw new NotImplementedException();
        }

        private static void HandleAirplaneDeparting(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneActorState> projectedAirplaneStates)
        {
            throw new NotImplementedException();
        }

        private static void HandleAirplaneTaxiing(IAirplane airplaneProxy, AirplaneActorState currentState, IDictionary<string, AirplaneActorState> projectedAirplaneStates)
        {
            throw new NotImplementedException();
        }
    }
}
