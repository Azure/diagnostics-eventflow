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

        public void Validate()
        {
            Verify.Operation(DeparturePoint != null, "Departure point must not be null");
            Verify.Operation(Destination != null, "Destination must not be null");
            Verify.Operation(DeparturePoint != Destination, "Departure point and destination cannot be the same");
            Verify.Operation(Universe.Current.Airports.Contains(DeparturePoint), "Unknown departure point airport");
            Verify.Operation(Universe.Current.Airports.Contains(Destination), "Unknown destination airport");
            Verify.Operation(!string.IsNullOrWhiteSpace(AirplaneID), "Airplane ID must not be empty");
        }
    }
}
