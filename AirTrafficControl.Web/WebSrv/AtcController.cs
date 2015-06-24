using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.WebSrv
{
    internal class AtcController
    {
        public async Task<IEnumerable<AirplaneStateModel>> GetFlyingAirplaneStates()
        {
            try
            {
                var retval = new List<AirplaneStateModel>();
                IAirTrafficControl atc = ActorProxy.Create<IAirTrafficControl>(new ActorId(WellKnownIdentifiers.SeattleCenter));
                var flyingAirplaneIDs = await atc.GetFlyingAirplaneIDs();

                if (flyingAirplaneIDs != null)
                {
                    foreach(string airplaneID in flyingAirplaneIDs)
                    {
                        var airplane = ActorProxy.Create<IAirplane>(new ActorId(airplaneID));
                        var airplaneActorState = await airplane.GetState();

                        AirplaneState airplaneState = airplaneActorState.AirplaneState;
                        Location airplaneLocation = null;

                        if (airplaneState is AirportLocationState)
                        {
                            airplaneLocation = ((AirportLocationState)airplaneState).Airport.Location;
                        }
                        else if (airplaneState is FixLocationState)
                        {
                            airplaneLocation = ((FixLocationState) airplaneState).Fix.Location;
                        }
                        else if (airplaneState is EnrouteState)
                        {
                            EnrouteState enrouteState = (EnrouteState) airplaneState;
                            airplaneLocation = new Location(
                                (enrouteState.To.Location.Latitude + enrouteState.From.Location.Latitude) / 2.0,
                                (enrouteState.To.Location.Longitude + enrouteState.From.Location.Longitude) / 2.0
                            );
                        }
                        else
                        {
                            throw new Exception("Unexpected airplane state, cannot determine location");
                        }

                        var stateModel = new AirplaneStateModel(airplaneID, airplaneActorState.AirplaneState.ToString(), airplaneLocation);
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
                return await airplane.GetState();
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
                flightPlan.DeparturePoint = Universe.Current.Airports.Where(a => a.Name == "KSEA").First();
                flightPlan.Destination = Universe.Current.Airports.Where(a => a.Name == "KPDX").First();
                flightPlan.Validate();

                IAirTrafficControl atc = ActorProxy.Create<IAirTrafficControl>(new ActorId(WellKnownIdentifiers.SeattleCenter));
                await atc.StartNewFlight(flightPlan);
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
    }
}
