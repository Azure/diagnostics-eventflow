using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace AirTrafficControl.Web.WebSrv
{
    internal class AtcController
    {
        private static Lazy<IHubContext<AtcHub>> AtcHubContext = new Lazy<IHubContext<AtcHub>>(() => 
            {
                var connectionManager = GlobalHost.DependencyResolver.Resolve<IConnectionManager>();
                var retval = connectionManager.GetHubContext<AtcHub>("atc");
                return retval;
            }, 
            System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        public async Task<IEnumerable<AirplaneStateDto>> GetFlyingAirplaneStates()
        {
            try
            {
                var retval = new List<AirplaneStateDto>();
                IAirTrafficControl atc = ActorProxy.Create<IAirTrafficControl>(new ActorId(WellKnownIdentifiers.SeattleCenter));
                var flyingAirplaneIDs = await atc.GetFlyingAirplaneIDs().ConfigureAwait(false);

                if (flyingAirplaneIDs != null)
                {
                    foreach(string airplaneID in flyingAirplaneIDs)
                    {
                        var airplane = ActorProxy.Create<IAirplane>(new ActorId(airplaneID));
                        var airplaneActorState = await airplane.GetState().ConfigureAwait(false);
                        var airplaneState = airplaneActorState.AirplaneState;

                        var stateModel = new AirplaneStateDto(airplaneID, airplaneState.ToString(), airplaneState.Location, airplaneState.GetHeading(airplaneActorState.FlightPlan));
                        retval.Add(stateModel);
                    }
                }

                return retval;
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.RestApiFrontEndError("GetFlyingAirplaneIDs", e.ToString());
                throw;
            }
        }

        public async Task<AirplaneActorState> GetAirplaneState(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("Airplane ID should not be empty", "id");
                }

                var airplane = ActorProxy.Create<IAirplane>(new ActorId(id));
                return await airplane.GetState().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.RestApiFrontEndError("GetAirplaneState", e.ToString());
                throw;
            }
        }

        public async Task StartNewFlight(string airplaneID, string departurePoint, string destination)
        {
            try
            {
                FlightPlan flightPlan = new FlightPlan();
                flightPlan.AirplaneID = airplaneID;
                flightPlan.DeparturePoint = Universe.Current.Airports.Where(a => a.Name == departurePoint).First();
                flightPlan.Destination = Universe.Current.Airports.Where(a => a.Name == destination).First();
                flightPlan.Validate();

                IAirTrafficControl atc = ActorProxy.Create<IAirTrafficControl>(new ActorId(WellKnownIdentifiers.SeattleCenter));
                await atc.StartNewFlight(flightPlan).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.RestApiFrontEndError("StartNewFlight", e.ToString());
                throw;
            }
        }

        public Task<IEnumerable<Airport>> GetAirports()
        {
            return Task.FromResult(Universe.Current.Airports.AsEnumerable());
        }

        public async Task PerformFlightStatusUpdate(IEnumerable<AirplaneStateDto> newAirplaneStates)
        {
            try
            {
                var context = AtcController.AtcHubContext.Value;
                await context.Clients.All.UpdateFlightStatus(newAirplaneStates);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.RestApiFrontEndError("PerformFlightStatusUpdate", e.ToString());
            }
        }
    }
}
