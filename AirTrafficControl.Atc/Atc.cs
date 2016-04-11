using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Wcf;
using Microsoft.ServiceFabric.Services.Communication.Client;
using AirTrafficControl.Common;
using Microsoft.ServiceFabric.Data;
using System.Diagnostics;
using Validation;

namespace AirTrafficControl.Atc
{
    internal sealed class Atc : StatefulService, IAirTrafficControl
    {
        private const string FrontendServiceName = "fabric:/AirTrafficControlApplication/AirTrafficControlWeb";

        private delegate Task AirplaneController(IAirplane airplaneProxy, AirplaneActorState airplaneActorState, IDictionary<string, AirplaneState> projectedAirplaneStates);

        private readonly IDictionary<Type, AirplaneController> AirplaneControllers;
        private ServicePartitionClient<HttpCommunicationClient> frontendCommunicationClient;
        private IReliableDictionary<string, int> flyingAirplaneIDs;

        public Atc(StatefulServiceContext context)
            : base(context)
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

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] {
                new ServiceReplicaListener( ctx => 
                    new WcfCommunicationListener<IAirTrafficControl>(ctx, this, WcfUtility.CreateTcpListenerBinding(), "AtcServiceEndpoint")
                )
            };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            this.flyingAirplaneIDs = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>(nameof(flyingAirplaneIDs));




            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private async Task HandleAirplaneLanded(IAirplane airplaneProxy, AirplaneActorState airplaneActorState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            // Just remove the airplane form the flying airplanes set
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                string airplaneID = airplaneActorState.FlightPlan.AirplaneID;

                var result = await this.flyingAirplaneIDs.TryRemoveAsync(tx, airplaneID);
                Debug.Assert(result.HasValue, $"Airplane {airplaneID} should be flying but we could not find it in the flying airplane dictionary");

                // Update the projected airplane state to "Unknown Location" to ensure we do not attempt to send any notifications about it.
                projectedAirplaneStates[airplaneActorState.FlightPlan.AirplaneID] = new UnknownLocationState();

                ServiceEventSource.Current.ServiceMessage(this, "ATC: Airplane {0} has landed and is no longer tracked", airplaneID);
            }
            
        }

        private Task HandleAirplaneApproaching(IAirplane airplaneProxy, AirplaneActorState airplaneActorState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            // We assume that every approach is successful, so just make a note that the airplane will be in the Landed state
            FlightPlan flightPlan = airplaneActorState.FlightPlan;
            Assumes.NotNull(flightPlan);
            projectedAirplaneStates[flightPlan.AirplaneID] = new LandedState(flightPlan.Destination);
            return Task.FromResult(true);
        }

        private async Task HandleAirplaneEnroute(IAirplane airplaneProxy, AirplaneActorState airplaneActorState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            EnrouteState enrouteState = (EnrouteState)airplaneActorState.AirplaneState;
            FlightPlan flightPlan = airplaneActorState.FlightPlan;

            if (enrouteState.To == flightPlan.Destination)
            {
                // Any other airplanes cleared for landing at this airport?
                if (projectedAirplaneStates.Values.OfType<ApproachState>().Any(state => state.Airport == flightPlan.Destination))
                {
                    projectedAirplaneStates[flightPlan.AirplaneID] = new HoldingState(flightPlan.Destination);
                    await airplaneProxy.ReceiveInstructionAsync(new HoldInstruction(flightPlan.Destination)).ConfigureAwait(false);
                    ServiceEventSource.Current.ServiceMessage(this, "ATC: Issued holding instruction for {0} at {1} because another airplane has been cleared for approach at the same airport",
                        flightPlan.AirplaneID, flightPlan.Destination.DisplayName);
                }
                else
                {
                    projectedAirplaneStates[flightPlan.AirplaneID] = new ApproachState(flightPlan.Destination);
                    await airplaneProxy.ReceiveInstructionAsync(new ApproachClearance(flightPlan.Destination)).ConfigureAwait(false);
                    ServiceEventSource.Current.ServiceMessage(this, "ATC: Issued approach clearance for {0} at {1}", flightPlan.AirplaneID, flightPlan.Destination.DisplayName);
                }
            }
            else
            {
                Fix nextFix = enrouteState.Route.GetNextFix(enrouteState.To, flightPlan.Destination);

                // Is another airplane destined to the same fix?
                if (projectedAirplaneStates.Values.OfType<EnrouteState>().Any(state => state.To == nextFix))
                {
                    // Hold at the end of the current route leg
                    projectedAirplaneStates[flightPlan.AirplaneID] = new HoldingState(enrouteState.To);
                    await airplaneProxy.ReceiveInstructionAsync(new HoldInstruction(enrouteState.To)).ConfigureAwait(false);
                    ServiceEventSource.Current.ServiceMessage(this, "ATC: Issued holding instruction for {0} at {1} because of traffic contention at {2}",
                        flightPlan.AirplaneID, enrouteState.To.DisplayName, nextFix.DisplayName);
                }
                else
                {
                    // Just let it proceed to next fix, no instruction necessary
                    projectedAirplaneStates[flightPlan.AirplaneID] = new EnrouteState(enrouteState.To, nextFix, enrouteState.Route);
                    ServiceEventSource.Current.ServiceMessage(this, "ATC: Airplane {0} is flying from {1} to {2}, next fix {3}",
                        flightPlan.AirplaneID, enrouteState.From.DisplayName, enrouteState.To.DisplayName, nextFix.DisplayName);
                }
            }
        }

        private async Task HandleAirplaneHolding(IAirplane airplaneProxy, AirplaneActorState airplaneActorState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            HoldingState holdingState = (HoldingState)airplaneActorState.AirplaneState;
            FlightPlan flightPlan = airplaneActorState.FlightPlan;

            // Case 1: airplane holding at destination airport
            if (holdingState.Fix == flightPlan.Destination)
            {
                // Grant approach clearance if no other airplane is cleared for approach at the same airport.
                if (!projectedAirplaneStates.Values.OfType<ApproachState>().Any(state => state.Airport == flightPlan.Destination))
                {
                    projectedAirplaneStates[flightPlan.AirplaneID] = new ApproachState(flightPlan.Destination);
                    await airplaneProxy.ReceiveInstructionAsync(new ApproachClearance(flightPlan.Destination)).ConfigureAwait(false);
                    ServiceEventSource.Current.ServiceMessage(this, "ATC: Airplane {0} has been cleared for approach at {1}", flightPlan.AirplaneID, flightPlan.Destination.DisplayName);
                }
                else
                {
                    projectedAirplaneStates[flightPlan.AirplaneID] = new HoldingState(flightPlan.Destination);
                    ServiceEventSource.Current.ServiceMessage(this, "ATC: Airplane {0} should continue holding at {1} because of other traffic landing",
                        flightPlan.AirplaneID, flightPlan.Destination.DisplayName);
                }

                return;
            }

            // Case 2: holding at some point enroute
            Route route = Universe.Current.GetRouteBetween(flightPlan.DeparturePoint, flightPlan.Destination);
            Assumes.NotNull(route);
            Fix nextFix = route.GetNextFix(holdingState.Fix, flightPlan.Destination);
            Assumes.NotNull(nextFix);

            if (projectedAirplaneStates.Values.OfType<EnrouteState>().Any(enrouteState => enrouteState.To == nextFix))
            {
                projectedAirplaneStates[flightPlan.AirplaneID] = holdingState;
                ServiceEventSource.Current.ServiceMessage(this, "ATC: Airplane {0} should continue holding at {1} because of traffic contention at {2}. Assuming compliance with previous instruction, no new instructions issued.",
                    flightPlan.AirplaneID, holdingState.Fix.DisplayName, nextFix.DisplayName);
            }
            else
            {
                projectedAirplaneStates[flightPlan.AirplaneID] = new EnrouteState(holdingState.Fix, nextFix, route);
                // We always optmimistically give an enroute clearance all the way to the destination
                await airplaneProxy.ReceiveInstructionAsync(new EnrouteClearance(flightPlan.Destination)).ConfigureAwait(false);
                ServiceEventSource.Current.ServiceMessage(this, "ATC: Airplane {0} should end holding at {1} and proceed to destination, next fix {2}. Issued new enroute clearance.",
                    flightPlan.AirplaneID, holdingState.Fix.DisplayName, nextFix.DisplayName);
            }
        }

        private async Task HandleAirplaneDeparting(IAirplane airplaneProxy, AirplaneActorState airplaneActorState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            DepartingState departingState = (DepartingState)airplaneActorState.AirplaneState;
            FlightPlan flightPlan = airplaneActorState.FlightPlan;

            Route route = Universe.Current.GetRouteBetween(flightPlan.DeparturePoint, flightPlan.Destination);
            Assumes.NotNull(route);
            Fix nextFix = route.GetNextFix(departingState.Airport, flightPlan.Destination);
            Assumes.NotNull(nextFix);

            if (projectedAirplaneStates.Values.OfType<EnrouteState>().Any(enrouteState => enrouteState.To == nextFix))
            {
                projectedAirplaneStates[flightPlan.AirplaneID] = new HoldingState(departingState.Airport);
                await airplaneProxy.ReceiveInstructionAsync(new HoldInstruction(departingState.Airport)).ConfigureAwait(false);
                ServiceEventSource.Current.ServiceMessage(this, "ATC: Issued holding instruction for {0} at {1} because of traffic contention at {2}",
                    flightPlan.AirplaneID, departingState.Airport.DisplayName, nextFix.DisplayName);
            }
            else
            {
                projectedAirplaneStates[flightPlan.AirplaneID] = new EnrouteState(departingState.Airport, nextFix, route);
                ServiceEventSource.Current.ServiceMessage(this, "ATC: Airplane {0} completed departure from {1} and proceeds enroute to destination, next fix {2}",
                    flightPlan.AirplaneID, departingState.Airport.DisplayName, nextFix.DisplayName);
            }
        }

        private async Task HandleAirplaneTaxiing(IAirplane airplaneProxy, AirplaneActorState airplaneActorState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            TaxiingState taxiingState = (TaxiingState)airplaneActorState.AirplaneState;
            FlightPlan flightPlan = airplaneActorState.FlightPlan;

            if (projectedAirplaneStates.Values.OfType<DepartingState>().Any(state => state.Airport == flightPlan.DeparturePoint))
            {
                projectedAirplaneStates[flightPlan.AirplaneID] = taxiingState;
                ServiceEventSource.Current.ServiceMessage(this, "ATC: Airplane {0} continue taxi at {1}, another airplane departing",
                    flightPlan.AirplaneID, flightPlan.DeparturePoint.DisplayName);
            }
            else
            {
                projectedAirplaneStates[flightPlan.AirplaneID] = new DepartingState(flightPlan.DeparturePoint);
                await airplaneProxy.ReceiveInstructionAsync(new TakeoffClearance(flightPlan.DeparturePoint)).ConfigureAwait(false);
                ServiceEventSource.Current.ServiceMessage(this, "ATC: Airplane {0} received takeoff clearance at {1}",
                    flightPlan.AirplaneID, flightPlan.DeparturePoint);
            }
        }

    }
}
