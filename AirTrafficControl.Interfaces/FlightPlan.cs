using Microsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Validation;

namespace AirTrafficControl.Interfaces
{
    [DataContract]
    public class FlightPlan
    {
        [DataMember]
        public Airport DeparturePoint { get; set; }

        [DataMember]
        public Airport Destination { get; set; }

        [DataMember]
        public string AirplaneID { get; set; }

        [DataMember]
        public IList<Fix> FlightPath { get; set; }

        public static void Validate(FlightPlan flightPlan, bool includeFlightPath = true)
        {
            Assumes.NotNull(flightPlan);
            Verify.Operation(flightPlan.DeparturePoint != null, "Departure point must not be null");
            Verify.Operation(flightPlan.Destination != null, "Destination must not be null");
            Verify.Operation(flightPlan.DeparturePoint != flightPlan.Destination, "Departure point and destination cannot be the same");
            Verify.Operation(Universe.Current.Airports.Contains(flightPlan.DeparturePoint), "Unknown departure point airport");
            Verify.Operation(Universe.Current.Airports.Contains(flightPlan.Destination), "Unknown destination airport");
            Verify.Operation(!string.IsNullOrWhiteSpace(flightPlan.AirplaneID), "Airplane ID must not be empty");
            
            if (includeFlightPath)
            {
                Verify.Operation(flightPlan.FlightPath != null, "Flight path should have been assigned by now");
                Verify.Operation(flightPlan.FlightPath.Count >= 2 && flightPlan.FlightPath[0].Name == flightPlan.DeparturePoint.Name 
                    && flightPlan.FlightPath[flightPlan.FlightPath.Count - 1].Name == flightPlan.Destination.Name,
                    "Flight path does not lead from departure point to destination");
            }
        }

        public Fix GetNextFix(Fix current)
        {
            Requires.NotNull(current, nameof(current));
            Validate(this);
            Requires.Argument(FlightPath.Contains(current), nameof(current), "The passed fix is not part of the current flight path");
            Requires.Argument(current != Destination, nameof(current), "The passed parameter is the destination fix for the fligh plan. There is no next fix after that.");

            int currentIndex = FlightPath.IndexOf(current);
            Fix next = FlightPath[currentIndex + 1];
            return next;
        }
    }
}
