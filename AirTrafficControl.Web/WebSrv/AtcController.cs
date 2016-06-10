using AirTrafficControl.Common;
using AirTrafficControl.Interfaces;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Validation;
using AtcServiceClient = System.Lazy<Microsoft.ServiceFabric.Services.Communication.Client.ServicePartitionClient<Microsoft.ServiceFabric.Services.Communication.Wcf.Client.WcfCommunicationClient<AirTrafficControl.Interfaces.IAirTrafficControl>>>;

namespace AirTrafficControl.Web.WebSrv
{
    internal class AtcController
    {
        

        private static Lazy<IHubContext<IAtcHubClient>> AtcHubContext = new Lazy<IHubContext<IAtcHubClient>>(() => 
            {
                var connectionManager = GlobalHost.DependencyResolver.Resolve<IConnectionManager>();
                var retval = connectionManager.GetHubContext<IAtcHubClient>("atc");
                return retval;
            }, 
            System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        private static AtcServiceClient AtcClient = new AtcServiceClient( AtcServiceClientFactory.CreateClient, LazyThreadSafetyMode.ExecutionAndPublication);

        public async Task<IEnumerable<AirplaneStateDto>> GetFlyingAirplaneStates()
        {
            try
            {
                var retval = new List<AirplaneStateDto>();

                IEnumerable<string> flyingAirplaneIDs = await AtcClient.Value.InvokeWithRetryAsync((client) => client.Channel.GetFlyingAirplaneIDs());

                if (flyingAirplaneIDs != null)
                {
                    foreach(string airplaneID in flyingAirplaneIDs)
                    {
                        var airplane = ActorProxy.Create<IAirplane>(new ActorId(airplaneID));
                        var airplaneActorState = await airplane.GetStateAsync();
                        var airplaneState = airplaneActorState.AirplaneState;
                        if (airplaneState == null)
                        {
                            // It can happen that the airplane was flying when we called ATC service, but by the time we ask the airplane for its state
                            // it has already landed and is no longer flying.
                            // If that is the case, just skip it.
                            continue;
                        }

                        var stateModel = new AirplaneStateDto(airplaneState, airplaneActorState.FlightPlan);
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
                
        public Task<AirplaneActorState> GetAirplaneState(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("Airplane ID should not be empty", "id");
                }

                var airplane = ActorProxy.Create<IAirplane>(new ActorId(id));
                return airplane.GetStateAsync();
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
                FlightPlan.Validate(flightPlan, includeFlightPath: false);

                await AtcClient.Value.InvokeWithRetryAsync((client) => client.Channel.StartNewFlight(flightPlan));
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

        public async Task PerformFlightStatusUpdate(FlightStatusModel flightStatusModel)
        {
            try
            {
                Requires.NotNull(flightStatusModel, nameof(flightStatusModel));
                var context = AtcController.AtcHubContext.Value;
                await context.Clients.All.flightStatusUpdate(flightStatusModel);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.RestApiFrontEndError("PerformFlightStatusUpdate", e.ToString());
            }
        }
    }
}
