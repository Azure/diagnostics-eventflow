using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
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
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Newtonsoft.Json;
using System.Net;

namespace AirTrafficControl.Atc
{
    internal sealed class Atc : StatefulService, IAirTrafficControl
    {
        private const string FrontendServiceName = "fabric:/AirTrafficControlApplication/AirTrafficControlWeb";
        private const string CurrentTimeProperty = "CurrentTime";
        private const string OperationNamePrefix = "AtcService/";

        private delegate Task AirplaneController(IAirplane airplaneProxy, AirplaneActorState airplaneActorState, IDictionary<string, AirplaneState> projectedAirplaneStates);

        private readonly IDictionary<Type, AirplaneController> AirplaneControllers;
        private ServicePartitionClient<HttpCommunicationClient> frontendCommunicationClient;
        private IReliableDictionary<string, int> flyingAirplaneIDs;
        private IReliableDictionary<string, string> serviceState;
        private Timer worldTimer;
        private readonly TimeSpan WorldTimerPeriod = TimeSpan.FromSeconds(5);

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

            var fabricContext = new FabricContext<StatefulServiceContext, IAirTrafficControl>(context, this);
            FabricContext<StatefulServiceContext, IAirTrafficControl>.Current = fabricContext;
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] {
                new ServiceReplicaListener( 
                    ctx => new WcfCommunicationListener<IAirTrafficControl>(ctx, this, WcfUtility.CreateTcpListenerBinding(), WellKnownIdentifiers.AtcServiceEndpointName),
                    WellKnownIdentifiers.AtcServiceListenerName)
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
            this.serviceState = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(nameof(serviceState));
            this.frontendCommunicationClient = new ServicePartitionClient<HttpCommunicationClient>(new HttpCommunicationClientFactory(), new Uri(FrontendServiceName));

            this.worldTimer?.Dispose();
            this.worldTimer = new Timer(OnTimePassed, null, TimeSpan.FromSeconds(1), WorldTimerPeriod);
        }

        public Task<IEnumerable<string>> GetFlyingAirplaneIDs()
        {
            return PerformServiceOperation<IEnumerable<string>>(OperationNamePrefix + nameof(GetFlyingAirplaneIDs), () =>
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    return GetFlyingAirplaneIDsInternal(tx);
                }
            });
        }

        public Task StartNewFlight(FlightPlan flightPlan)
        {
            return PerformServiceOperation<bool>(OperationNamePrefix + nameof(StartNewFlight), async () => 
            {
                FlightPlan.Validate(flightPlan, includeFlightPath: false);
                flightPlan.FlightPath = Dispatcher.ComputeFlightPath(flightPlan.DeparturePoint, flightPlan.Destination);

                using (var tx = this.StateManager.CreateTransaction())
                {
                    if ((await this.flyingAirplaneIDs.TryGetValueAsync(tx, flightPlan.AirplaneID)).HasValue)
                    {
                        // In real life airplanes can have multiple flight plans filed, just for different times. But here we assume there can be only one flight plan per airplane
                        throw new InvalidOperationException("The airplane " + flightPlan.AirplaneID + " is already flying");
                        // CONSIDER forcing execution of the new flight plan here, instead of throwing an error.
                    }

                    ActorId actorID = new ActorId(flightPlan.AirplaneID);
                    IAirplane airplane = ActorProxy.Create<IAirplane>(actorID);
                    await airplane.StartFlightAsync(flightPlan);
                    await this.flyingAirplaneIDs.AddAsync(tx, flightPlan.AirplaneID, 0);
                    await tx.CommitAsync();

                    ServiceEventSource.Current.ServiceMessage(this, "ATC: new filght plan received for {0}: departing from {1}, destination {2}. Flight path is {3}",
                        flightPlan.AirplaneID,
                        flightPlan.DeparturePoint.DisplayName,
                        flightPlan.Destination.DisplayName,
                        flightPlan.FlightPath);
                }
                return true;
            });
        }

        public Task<long> GetFlyingAirplaneCount()
        {
            return PerformServiceOperation<long>(OperationNamePrefix + nameof(GetFlyingAirplaneCount), async () =>
             {
                 using (var tx = this.StateManager.CreateTransaction())
                 {
                     long flyingAirplaneCount = await this.flyingAirplaneIDs.GetCountAsync(tx);
                     return flyingAirplaneCount;
                 }
             });
        }

        private async Task<T> PerformServiceOperation<T>(string operationName, Func<Task<T>> impl)
        {
            Requires.NotNullOrWhiteSpace(operationName, nameof(operationName));

            string correlationId = Guid.NewGuid().ToString();
            ServiceEventSource.Current.ServiceRequestStart(operationName, correlationId);
            Exception unexpectedException = null;
            DateTime startTimeUtc = DateTime.UtcNow;
            try
            {
                T retval = await impl();
                return retval;
            } 
            catch (Exception e)
            {
                unexpectedException = e;                
                throw;
            }
            finally
            {
                HttpStatusCode statusCode = unexpectedException == null ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;

                ServiceEventSource.Current.ServiceRequestStop(
                    operationName,
                    ServiceContext,
                    correlationId,
                    startTimeUtc,
                    DateTime.UtcNow - startTimeUtc,
                    statusCode,
                    unexpectedException?.ToString() ?? string.Empty);
            }
        }

        private void OnTimePassed(object timerState)
        {
            Task.Run(async () => 
            {
                int currentTime;
                IEnumerable<string> flyingAirplaneIDs;

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    flyingAirplaneIDs = await GetFlyingAirplaneIDsInternal(tx);
                    currentTime = await GetCurrentTime(tx);
                    await SetCurrentTime(tx, ++currentTime);
                    await tx.CommitAsync();
                }

                if (!flyingAirplaneIDs.Any())
                {
                    return; // Nothing else to do
                }

                var airplaneProxies = CreateAirplaneProxies(flyingAirplaneIDs);
                var airplaneActorStatesByDepartureTime = (await Task.WhenAll(flyingAirplaneIDs.Select(id => airplaneProxies[id].GetStateAsync())))
                                                            .Where(state => !(state.AirplaneState is UnknownLocationState))
                                                            .OrderBy(state => (state.AirplaneState is TaxiingState) ? int.MaxValue : state.DepartureTime);
                var newAirplaneStates = new Dictionary<string, AirplaneState>();

                foreach (var airplaneActorState in airplaneActorStatesByDepartureTime)
                {
                    var controllerFunction = this.AirplaneControllers[airplaneActorState.AirplaneState.GetType()];
                    Assumes.NotNull(controllerFunction);

                    await controllerFunction(airplaneProxies[airplaneActorState.FlightPlan.AirplaneID], airplaneActorState, newAirplaneStates);
                }

                await Task.WhenAll(newAirplaneStates.Keys.Select(airplaneID => airplaneProxies[airplaneID].TimePassedAsync(currentTime)));

                // Notify anybody who is listening about new airplane states
                var airplaneStateNotifications = airplaneActorStatesByDepartureTime
                    .Where(airplaneActorState => !(newAirplaneStates[airplaneActorState.FlightPlan.AirplaneID] is UnknownLocationState))
                    .Select(airplaneActorState => new AirplaneStateDto(newAirplaneStates[airplaneActorState.FlightPlan.AirplaneID], airplaneActorState.FlightPlan));

                NotifyFlightStatus(airplaneStateNotifications);
            });
        }

        private async Task<IEnumerable<string>> GetFlyingAirplaneIDsInternal(ITransaction tx)
        {
            var retval = new List<string>();
            var flyingAirplaneIDsEnumerator = (await this.flyingAirplaneIDs.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

            while (await flyingAirplaneIDsEnumerator.MoveNextAsync(CancellationToken.None))
            {
                retval.Add(flyingAirplaneIDsEnumerator.Current.Key);
            }

            return retval;
        }

        private Dictionary<string, IAirplane> CreateAirplaneProxies(IEnumerable<string> flyingAirplaneIDs)
        {
            var retval = new Dictionary<string, IAirplane>();
            foreach (var airplaneID in flyingAirplaneIDs)
            {
                retval.Add(airplaneID, ActorProxy.Create<IAirplane>(new ActorId(airplaneID)));
            }
            return retval;
        }

        private void NotifyFlightStatus(IEnumerable<AirplaneStateDto> airplaneStateNotifications)
        {
            try
            {
                this.frontendCommunicationClient.InvokeWithRetryAsync(async communicationClient =>
                {
                    try
                    {
                        var flightStatusModel = new FlightStatusModel();
                        flightStatusModel.AirplaneStates = airplaneStateNotifications;
                        flightStatusModel.EstimatedNextStatusUpdateDelayMsec = WorldTimerPeriod.TotalMilliseconds;
                        var content = new StringContent(JsonConvert.SerializeObject(flightStatusModel), System.Text.Encoding.UTF8, "application/json");
                        await communicationClient.HttpClient.PostAsync(new Uri(communicationClient.Url, "/api/notify/flight-status"), content);

                        ServiceEventSource.Current.ServiceMessage(this, "Flight status notification sent");
                    }
                    catch (Exception e)
                    {
                        ServiceEventSource.Current.FlightStatusNotificationFailed(e.ToString());
                        throw;
                    }
                });
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.FlightStatusNotificationFailed(e.ToString());
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

                int currentTime = await GetCurrentTime(tx);

                await tx.CommitAsync();

                ServiceEventSource.Current.FlightCompleted(
                    airplaneID,
                    airplaneActorState.FlightPlan.DeparturePoint.Name,
                    airplaneActorState.FlightPlan.Destination.Name,
                    currentTime - airplaneActorState.DepartureTime,
                    ServiceContext);
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
                Fix nextFix = flightPlan.GetNextFix(enrouteState.To);

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
                    projectedAirplaneStates[flightPlan.AirplaneID] = new EnrouteState(enrouteState.To, nextFix);
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
            Fix nextFix = flightPlan.GetNextFix(holdingState.Fix);

            if (projectedAirplaneStates.Values.OfType<EnrouteState>().Any(enrouteState => enrouteState.To == nextFix))
            {
                projectedAirplaneStates[flightPlan.AirplaneID] = holdingState;
                ServiceEventSource.Current.ServiceMessage(this, "ATC: Airplane {0} should continue holding at {1} because of traffic contention at {2}. Assuming compliance with previous instruction, no new instructions issued.",
                    flightPlan.AirplaneID, holdingState.Fix.DisplayName, nextFix.DisplayName);
            }
            else
            {
                projectedAirplaneStates[flightPlan.AirplaneID] = new EnrouteState(holdingState.Fix, nextFix);
                // We always optmimistically give an enroute clearance all the way to the destination
                await airplaneProxy.ReceiveInstructionAsync(new EnrouteClearance(flightPlan.Destination, flightPlan.FlightPath));
                ServiceEventSource.Current.ServiceMessage(this, "ATC: Airplane {0} should end holding at {1} and proceed to destination, next fix {2}. Issued new enroute clearance.",
                    flightPlan.AirplaneID, holdingState.Fix.DisplayName, nextFix.DisplayName);
            }
        }

        private async Task HandleAirplaneDeparting(IAirplane airplaneProxy, AirplaneActorState airplaneActorState, IDictionary<string, AirplaneState> projectedAirplaneStates)
        {
            DepartingState departingState = (DepartingState)airplaneActorState.AirplaneState;
            FlightPlan flightPlan = airplaneActorState.FlightPlan;

            Fix nextFix = flightPlan.GetNextFix(departingState.Airport);

            if (projectedAirplaneStates.Values.OfType<EnrouteState>().Any(enrouteState => enrouteState.To == nextFix))
            {
                projectedAirplaneStates[flightPlan.AirplaneID] = new HoldingState(departingState.Airport);
                await airplaneProxy.ReceiveInstructionAsync(new HoldInstruction(departingState.Airport)).ConfigureAwait(false);
                ServiceEventSource.Current.ServiceMessage(this, "ATC: Issued holding instruction for {0} at {1} because of traffic contention at {2}",
                    flightPlan.AirplaneID, departingState.Airport.DisplayName, nextFix.DisplayName);
            }
            else
            {
                projectedAirplaneStates[flightPlan.AirplaneID] = new EnrouteState(departingState.Airport, nextFix);
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

        private Task<int> GetCurrentTime(ITransaction tx)
        {
            return this.serviceState.GetOrAddAsync(tx, CurrentTimeProperty, "0").ContinueWith<int>(currentTimeLoadTask => int.Parse(currentTimeLoadTask.Result));
        }

        private Task SetCurrentTime(ITransaction tx, int newTime)
        {
            return this.serviceState.AddOrUpdateAsync(tx, CurrentTimeProperty, "0", (key, currentTime) => newTime.ToString());
        }

        private StatefulServiceContext ServiceContext
        {
            get { return FabricContext<StatefulServiceContext, IAirTrafficControl>.Current.ServiceContext; }
        }
    }
}
