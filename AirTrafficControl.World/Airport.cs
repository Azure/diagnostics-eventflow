using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace AirTrafficControl.World
{
    #pragma warning disable 0659

    [DataContract]
    public class Airport: Fix
    {
        public Airport(string name, string displayName, Direction publishedHoldBearing): base(name, displayName)
        {
            this.PublishedHoldBearing = publishedHoldBearing;
        }

        [DataMember]
        public ReadOnlyCollection<Route> RouteConnections { get; set; }

        [DataMember]
        public Direction PublishedHoldBearing { get; private set; }

        public override bool Equals(object obj)
        {
            Airport other = obj as Airport;
            if (other == null)
            {
                return false;
            }

            return this.Name == other.Name;
        }
    }

    #pragma warning restore 0659
}
