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

        
        private IReliableDictionary<string, string> serviceState;        
        private AtcServiceClient AtcClient = new AtcServiceClient(AtcServiceClientFactory.CreateClient, LazyThreadSafetyMode.ExecutionAndPublication);

        private bool updatingSimulatedTraffic = false;
        private Timer trafficSimulationTimer;
        private byte simulatedTrafficCount = 0;
        private object instanceLock = new object();
        private bool primaryShuttingDown = false;

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
            this.trafficSimulationTimer?.Dispose();
            this.primaryShuttingDown = false;
            
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                this.serviceState = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(tx, nameof(serviceState));
                await tx.CommitAsync();
            }

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                this.simulatedTrafficCount = await GetSimulatedTrafficCount(tx);
                await tx.CommitAsync();
            }

            lock (this.instanceLock)
            {
                if (this.simulatedTrafficCount > 0)
                {
                    this.trafficSimulationTimer = CreateTrafficSimulationTimer();
                }
            }

            while(true)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                if (cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        this.primaryShuttingDown = true;
                        this.trafficSimulationTimer?.Dispose();
                        this.trafficSimulationTimer = null;
                    }
                    catch { }

                    break;
                }
            }
        }

        public async Task ChangeTrafficSimulation(TrafficSimulationModel trafficSimulationSettings)
        {
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await SetSimulatedTrafficCount(tx, trafficSimulationSettings.SimulatedTrafficCount);
                ServiceEventSource.Current.ServiceMessage(this, $"Updating the desired number of simulated airplanes to {trafficSimulationSettings.SimulatedTrafficCount}");
                await tx.CommitAsync();
            }

            lock (this.instanceLock)
            {
                this.simulatedTrafficCount = trafficSimulationSettings.SimulatedTrafficCount;
                if (trafficSimulationSettings.SimulatedTrafficCount == 0 || this.primaryShuttingDown)
                {
                    this.trafficSimulationTimer?.Dispose();
                    this.trafficSimulationTimer = null;
                }
                else if (this.trafficSimulationTimer == null)
                {
                    this.trafficSimulationTimer = CreateTrafficSimulationTimer();
                }
            }
        }

        private void SimulateTraffic(object state)
        {
            Task.Run(async () =>
            {
                byte desiredTrafficCount = this.simulatedTrafficCount;
                if (desiredTrafficCount == 0)
                {
                    return; // Nothing to do
                }

                if (this.updatingSimulatedTraffic)
                {
                    return; // For some reason updating simulated traffic took longer. We do not want to run two or more updates concurrently.
                }

                try
                {
                    this.updatingSimulatedTraffic = true;

                    long flyingAirplaneCount = await this.AtcClient.Value.InvokeWithRetryAsync(client => client.Channel.GetFlyingAirplaneCount());
                    if (flyingAirplaneCount < desiredTrafficCount)
                    {
                        byte newFlightsCount = (byte)(desiredTrafficCount - flyingAirplaneCount);
                        if (newFlightsCount > 0)
                        {
                            ServiceEventSource.Current.ServiceMessage(this, $"Launching {newFlightsCount} new flights to maintain the desired number of at least {desiredTrafficCount} flights in the air");
                        }

                        var randomGen = new Random();

                        for (byte i = 0; i < newFlightsCount; i++)
                        {
                            await LaunchNewFlight(randomGen);
                        }
                    }
                }
                finally
                {
                    this.updatingSimulatedTraffic = false;
                }
            });
        }

        private async Task LaunchNewFlight(Random randomGen)
        {
            var airports = Universe.Current.Airports;
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
            return new Timer(SimulateTraffic, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
        }
    }
}
