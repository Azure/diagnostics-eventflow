using AirTrafficControl.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Interfaces
{
    [DataContract]
    public class FlightPlan
    {
        [DataMember]
        public Airport DeparturePoint { get; set; }

        [DataMember]
        public Airport Destination { get; set; }
    }
}
