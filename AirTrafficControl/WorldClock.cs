using AirTrafficControl.Common;
using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Validation;

namespace AirTrafficControl
{
    [StatePersistence(StatePersistence.Persisted)]
    public class WorldClock : Actor, IRemindable
    {
        private const string TimePassedReminder = "AirTrafficControl.TimePassedReminder";
        private const string CurrentTimeStateProperty = "CurrentTime";    

        
    
        public WorldClock(): base()
        {
        }        

        protected override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();            
            await this.StateManager.TryAddStateAsync<int>(CurrentTimeStateProperty, 0);            
        }

        public async Task StartNewFlight(FlightPlan flightPlan)
        {
            Requires.NotNull(flightPlan, "flightPlan");
            flightPlan.Validate();

            var flyingAirplaneIDs = await GetFlyingAirplaneIDsInternal();

            if (flyingAirplaneIDs.Contains(flightPlan.AirplaneID))
            {
                // In real life airplanes can have multiple flight plans filed, just for different times. But here we assume there can be only one flight plan per airplane
                throw new InvalidOperationException("The airplane " + flightPlan.AirplaneID + " is already flying");
            }

            // Make sure we have a reminder set up so that we can simulate the flight
            IActorReminder reminder = null;
            try
            {
                reminder = this.GetReminder(TimePassedReminder);
            }
            catch { }
            if (reminder == null)
            {
                int currentTime = await this.StateManager.GetStateAsync<int>(CurrentTimeStateProperty);
                ActorEventSource.Current.ActorMessage(this, "ATC: Starting the world timer, current time is {0}", currentTime);                
                await this.RegisterReminderAsync(TimePassedReminder, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10));
            }

            ActorId actorID = new ActorId(flightPlan.AirplaneID);
            IAirplane airplane = ActorProxy.Create<IAirplane>(actorID);
            await airplane.StartFlightAsync(flightPlan);
            flyingAirplaneIDs.Add(flightPlan.AirplaneID);

            await SetFlyingAirplaneIDs(flyingAirplaneIDs);

            ActorEventSource.Current.ActorMessage(this, "ATC: new filght plan received for {0}: departing from {1}, destination {2}.",
                flightPlan.AirplaneID,
                flightPlan.DeparturePoint.DisplayName,
                flightPlan.Destination.DisplayName);
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            if (!TimePassedReminder.Equals(reminderName, StringComparison.Ordinal))
            {
                return;
            }

            
        }

        private Dictionary<string, IAirplane> CreateAirplaneProxies(List<string> flyingAirplaneIDs)
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
                                var content = new StringContent(JsonConvert.SerializeObject(airplaneStateNotifications), System.Text.Encoding.UTF8, "application/json");
                                await communicationClient.HttpClient.PostAsync(new Uri(communicationClient.Url, "/api/notify/flight-status"), content);

                                ActorEventSource.Current.ActorMessage(this, "Flight status notification sent");
                            }
                            catch (Exception e)
                            {
                                ActorEventSource.Current.FlightStatusNotificationFailed(e.ToString());
                                throw;
                            }
                        });
            }
            catch (Exception e)
            {
                ActorEventSource.Current.FlightStatusNotificationFailed(e.ToString());
            }
        }

        
    }
}
