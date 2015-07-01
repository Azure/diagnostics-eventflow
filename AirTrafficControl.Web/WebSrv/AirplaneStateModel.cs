using AirTrafficControl.Interfaces;
using Microsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.WebSrv
{
    internal class AirplaneStateModel
    {
        public AirplaneStateModel(string id, string stateDescription, Location position, double heading)
        {
            Requires.NotNullOrWhiteSpace(id, "id");

            this.ID = id;
            this.StateDescription = stateDescription;
            this.Location = position;
            this.Heading = heading;
        }

        public string ID { get; private set; }
        public string StateDescription { get; private set; }

        public Location Location { get; private set; }

        // Heading (in radians), 360 is zero and increases clockwise
        public double Heading { get; private set; }
    }
}
