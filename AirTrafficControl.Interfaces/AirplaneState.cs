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
    public abstract class AirplaneState
    {
    }

    [DataContract]
    public abstract class AirportLocationState : AirplaneState
    {
        [DataMember]
        public Airport Airport { get; set; }
    }

    [DataContract]
    public abstract class FixLocationState : AirplaneState
    {
        [DataMember]
        public Fix Fix { get; set; }
    }

    [DataContract]
    public class Taxiing : AirportLocationState
    {
        public override string ToString()
        {
            return "Taxiing at " + Airport.DisplayName;
        }
    }

    [DataContract]
    public class Departing : AirportLocationState
    {
        public override string ToString()
        {
            return "Departing from " + Airport.DisplayName;
        }
    }

    [DataContract]
    public class Holding : FixLocationState
    {
        public override string ToString()
        {
            return "Holding at " + Fix.DisplayName;
        }
    }

    [DataContract]
    public class Approach: AirportLocationState
    {
        public override string ToString()
        {
            return "Flying approach to " + Airport.DisplayName;
        }
    }

    [DataContract]
    public class Landed: AirportLocationState
    {
        public override string ToString()
        {
            return "Landed at " + Airport.DisplayName;
        }
    }

    [DataContract]
    public class Enroute: AirplaneState
    {
        [DataMember]
        public Fix From { get; set; }

        [DataMember]
        public Fix To { get; set; }

        [DataMember]
        public Route Route { get; set; }

        public override string ToString()
        {
            return "Flying on route " + Route.Name + " from " + From.DisplayName + " to " + To.DisplayName;
        }
    }
}
