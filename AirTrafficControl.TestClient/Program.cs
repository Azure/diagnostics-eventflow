using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using System;
using System.Linq;

namespace AirTrafficControl.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            IAirTrafficControl atc = ActorProxy.Create<IAirTrafficControl>(new ActorId(WellKnownIdentifiers.SeattleCenter), "fabric:/AirTrafficControlApplication");

            FlightPlan flightPlan = new FlightPlan();
            flightPlan.AirplaneID = "489Y";
            // From Seattle to Portland
            flightPlan.DeparturePoint = Universe.Current.Airports.Where(a => a.Name == "KSEA").First();
            flightPlan.Destination = Universe.Current.Airports.Where(a => a.Name == "KPDX").First();
            atc.StartNewFlight(flightPlan).Wait();

            Console.WriteLine("Flight started. Press any key to exit");
            Console.ReadKey();
        }
    }
}
