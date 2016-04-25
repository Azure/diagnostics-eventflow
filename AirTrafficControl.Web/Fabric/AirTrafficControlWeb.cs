using AirTrafficControl.Common;
using AirTrafficControl.Interfaces;
using AirTrafficControl.Web.TrafficSimulator;
using AirTrafficControl.Web.WebSrv;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using AtcServiceClient = System.Lazy<Microsoft.ServiceFabric.Services.Communication.Client.ServicePartitionClient<Microsoft.ServiceFabric.Services.Communication.Wcf.Client.WcfCommunicationClient<AirTrafficControl.Interfaces.IAirTrafficControl>>>;

namespace AirTrafficControl.Web.Fabric
{
    public class AirTrafficControlWeb : StatefulService, ITrafficSimulator
    {
        private const string SimulatedTrafficCountProperty = "CurrentTime";

        private Timer trafficSimulationTimer;
        private IReliableDictionary<string, string> serviceState;
        private byte? simulatedTrafficCount;
        private AtcServiceClient AtcClient = new AtcServiceClient(AtcServiceClientFactory.CreateClient, LazyThreadSafetyMode.ExecutionAndPublication);        

        public AirTrafficControlWeb(StatefulServiceContext ctx): base(ctx) { }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] { new ServiceReplicaListener(CreateCommunicationListener) };
        }

        private ICommunicationListener CreateCommunicationListener(StatefulServiceContext ctx)
        {
            var fabricContext = new FabricContext<StatefulServiceContext, ITrafficSimulator>(ctx, this);
            FabricContext<StatefulServiceContext, ITrafficSimulator>.Current = fabricContext;

            var listener = new OwinCommunicationListener(new OwinStartup(), ctx);

            ServiceEventSource.Current.ServiceMessage(this, "Communication listener created");
            return listener;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            this.serviceState = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(nameof(serviceState));

            this.trafficSimulationTimer?.Dispose();
            this.trafficSimulationTimer = CreateTrafficSimulationTimer();
        }

        public async Task ChangeTrafficSimulation(TrafficSimulationModel trafficSimulationSettings)
        {
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await SetSimulatedTrafficCount(tx, trafficSimulationSettings.SimulatedTrafficCount);
                ServiceEventSource.Current.ServiceMessage(this, $"Updating the desired number of simulated airplanes to {trafficSimulationSettings.SimulatedTrafficCount}");
                await tx.CommitAsync();
            }

            this.simulatedTrafficCount = trafficSimulationSettings.SimulatedTrafficCount;
            if (trafficSimulationSettings.SimulatedTrafficCount == 0)
            {
                this.trafficSimulationTimer?.Dispose();
                this.trafficSimulationTimer = null;
            }
            else if (this.trafficSimulationTimer == null)
            {
                this.trafficSimulationTimer = CreateTrafficSimulationTimer();
            }
        }

        private void SimulateTraffic(object state)
        {
            Task.Run(async () =>
            {
                if (this.simulatedTrafficCount == null)
                {
                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        this.simulatedTrafficCount = await GetSimulatedTrafficCount(tx);
                    }
                }

                long flyingAirplaneCount = await this.AtcClient.Value.InvokeWithRetryAsync(client => client.Channel.GetFlyingAirplaneCount());
                if (flyingAirplaneCount < this.simulatedTrafficCount.Value)
                {
                    byte newFlightsCount = (byte) (this.simulatedTrafficCount.Value - flyingAirplaneCount);
                    var randomGen = new Random();
                    var airports = Universe.Current.Airports;
                    if (newFlightsCount > 0)
                    {
                        ServiceEventSource.Current.ServiceMessage(this, $"Launching {newFlightsCount} new flights to maintain the desired number of at least {this.simulatedTrafficCount.Value} flights in the air");
                    }

                    for (byte i = 0; i < newFlightsCount; i++)
                    {
                        Airport departureAirport = airports[randomGen.Next(airports.Count)];
                        Airport destinationAirport = null;
                        do
                        {
                            destinationAirport = airports[randomGen.Next(airports.Count)];
                        } while (departureAirport == destinationAirport);

                        IEnumerable<string> flyingAirplanes = await this.AtcClient.Value.InvokeWithRetryAsync(client => client.Channel.GetFlyingAirplaneIDs());
                        string newAirplaneID = null;
                        do
                        {
                            newAirplaneID = "N" + randomGen.Next(1, 1000).ToString() + "SIM";
                        } while (flyingAirplanes.Contains(newAirplaneID));

                        var flightPlan = new FlightPlan();
                        flightPlan.AirplaneID = newAirplaneID;
                        flightPlan.DeparturePoint = departureAirport;
                        flightPlan.Destination = destinationAirport;
                        await this.AtcClient.Value.InvokeWithRetryAsync(client => client.Channel.StartNewFlight(flightPlan));
                    }
                }
            });
        }

        private Task<byte> GetSimulatedTrafficCount(ITransaction tx)
        {
            return this.serviceState.GetOrAddAsync(tx, SimulatedTrafficCountProperty, "0")
                .ContinueWith<byte>(simulatedTrafficCountLoadTask => byte.Parse(simulatedTrafficCountLoadTask.Result));
        }

        private Task SetSimulatedTrafficCount(ITransaction tx, byte newTrafficCount)
        {
            return this.serviceState.AddOrUpdateAsync(tx, SimulatedTrafficCountProperty, "0", (key, currentTrafficCount) => newTrafficCount.ToString());
        }

        private Timer CreateTrafficSimulationTimer()
        {
            return new Timer(SimulateTraffic, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
        }
    }
}
