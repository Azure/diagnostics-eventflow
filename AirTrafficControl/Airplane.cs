using AirTrafficControl.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl
{
    public class Airplane : IAirplane
    {
        public Task TimePassed()
        {
            throw new NotImplementedException();
        }
    }

    [DataContract]
    public class AirplaneActorState
    {
        public AirplaneState AirplaneState { get; set; }

        public FlightPlan FlightPlan { get; set; }

        public 
    }
}
