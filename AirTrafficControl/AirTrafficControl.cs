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
    public class AirTrafficControl : Actor<AirTrafficControlState>, IAirTrafficControl
    {
        public Task<IEnumerable<string>> GetFlyingAirplaneIDs()
        {
            return Task.FromResult(this.State.FlyingAirplaneIDs.AsEnumerable());
        }

        public Task StartNewFlight(string airplaneID, FlightPlan flightPlan)
        {
            Requires.NotNullOrWhiteSpace(airplaneID, "airplaneID");
            Requires.NotNull(flightPlan, "flightPlan");

            // TODO: validate that filghtPlan 
            ActorId actorID = new ActorId(airplaneID);
            IAirplane airplane = ActorProxy.Create<IAirplane>(actorID);
            airplane.StartFlight(flightPlan);
        }
    }
}
