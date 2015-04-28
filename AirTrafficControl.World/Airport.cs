using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace AirTrafficControl.World
{
    #pragma warning disable 0659

    public class Airport: Fix
    {
        public Airport(string name, string displayName, Direction publishedHoldBearing): base(name, displayName)
        {
            this.PublishedHoldBearing = publishedHoldBearing;
        }

        public ReadOnlyCollection<Route> RouteConnections { get; set; }
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
