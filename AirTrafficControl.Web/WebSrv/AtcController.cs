using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System.Threading;
using System.ServiceModel;

namespace AirTrafficControl.Web.WebSrv
{
    internal class AtcController
    {
        private static readonly Uri AtcServiceUri = new Uri("fabric:/AirTrafficControlApplication/Atc");

        private static Lazy<IHubContext<IAtcHubClient>> AtcHubContext = new Lazy<IHubContext<IAtcHubClient>>(() => 
            {
                var connectionManager = GlobalHost.DependencyResolver.Resolve<IConnectionManager>();
                var retval = connectionManager.GetHubContext<IAtcHubClient>("atc");
                return retval;
            }, 
            System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        private static IAirTrafficControl AtcClient;
        private static object AtcClientLock = new object();

        public async Task<IEnumerable<AirplaneStateDto>> GetFlyingAirplaneStates()
        {
            try
            {
                var retval = new List<AirplaneStateDto>();
                IAirTrafficControl atc = await GetAtcClient();
                var flyingAirplaneIDs = await atc.GetFlyingAirplaneIDs().ConfigureAwait(false);

                if (flyingAirplaneIDs != null)
                {
                    foreach(string airplaneID in flyingAirplaneIDs)
                    {
                        var airplane = ActorProxy.Create<IAirplane>(new ActorId(airplaneID));
                        var airplaneActorState = await airplane.GetStateAsync();
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

                IAirTrafficControl atc = await GetAtcClient();
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

        public async Task PerformFlightStatusUpdate(IEnumerable<AirplaneStateDto> newAirplaneStates)
        {
            try
            {
                var context = AtcController.AtcHubContext.Value;
                await context.Clients.All.flightStatusUpdate(newAirplaneStates);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.RestApiFrontEndError("PerformFlightStatusUpdate", e.ToString());
            }
        }

        private async Task<IAirTrafficControl> GetAtcClient()
        {
            lock (AtcClientLock)
            {
                if (IsUsable(AtcClient as ICommunicationObject))
                {
                    return AtcClient;
                }
            }

            var wcfClientFactory = new WcfCommunicationClientFactory<IAirTrafficControl>(
                    clientBinding: WcfUtility.CreateTcpClientBinding(),
                    servicePartitionResolver: ServicePartitionResolver.GetDefault());

            var newAtcClient = await wcfClientFactory.GetClientAsync(
                AtcServiceUri,
                ServicePartitionKey.Singleton,
                TargetReplicaSelector.Default,
                WellKnownIdentifiers.AtcServiceListenerName,
                new OperationRetrySettings(),
                CancellationToken.None
                );

            lock (AtcClientLock)
            {
                if (!IsUsable(AtcClient as ICommunicationObject))
                {
                    AtcClient = (IAirTrafficControl) newAtcClient.Channel;
                }

                return AtcClient;
            }
        }

        private bool IsUsable(ICommunicationObject co)
        {
            if (co == null)
            {
                return false;
            }

            switch (co.State)
            {
                case CommunicationState.Opened:
                case CommunicationState.Opening:
                case CommunicationState.Created:
                    return true;

                default:
                    return false;
            }
        }
    }
}
