using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Interfaces
{
    [DataContract]
    public class Location
    {
        public Location(double latitude, double longitude) : this(latitude, longitude, null) { }

        public Location(double latitude, double longitude, double? altitude)
        {
            if (latitude < -90.0 || latitude > 90.0)
            {
                throw new ArgumentOutOfRangeException("latitude");
            }

            if (longitude < -180.0 || longitude > 180.0)
            {
                throw new ArgumentOutOfRangeException("longitude");
            }

            this.Latitude = latitude;
            this.Longitude = longitude;
            this.Altitude = altitude;
        }

        [DataMember]
        public double Latitude { get; private set; }

        [DataMember]
        public double Longitude { get; private set; }

        [DataMember]
        public double? Altitude { get; private set; }

        public double GetDirectHeadingTo(Location another)
        {
            if (another == null) { throw new ArgumentNullException("another"); }

            double dLon = another.Longitude - this.Longitude;
            double dLat = another.Latitude - this.Latitude;
            double heading = Math.PI / 2.0 - Math.Atan2(dLat, dLon);
            if (heading < 0)
            {
                heading += 2 * Math.PI;
            }
            return heading;
        }
    }
}
