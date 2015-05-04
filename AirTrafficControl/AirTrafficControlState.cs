using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;

namespace AirTrafficControl
{
    [DataContract]
    public class AirTrafficControlState
    {
        [DataMember]
        public List<string> FlyingAirplaneIDs { get; set; }

        [DataMember]
        public int CurrentTime { get; set; }
    }
}