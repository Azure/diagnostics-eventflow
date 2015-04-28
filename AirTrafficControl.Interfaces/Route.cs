using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace AirTrafficControl.Interfaces
{
    [DataContract]
    public class Route
    {
        public Route(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException();
            }

            Name = name;
        }

        [DataMember]
        public string Name { get; private set; }

        [DataMember]
        public ReadOnlyCollection<Fix> Fixes { get; set; }

        public Fix GetNextFix(Fix currentFix, Fix destinationFix)
        {
            int currentFixIndex = Fixes.IndexOf(currentFix);
            if (currentFixIndex < 0)
            {
                throw new ArgumentException("The current fix is not part of the route", "currentFix");
            }

            int destinationFixIndex = Fixes.IndexOf(destinationFix);
            if (destinationFixIndex < 0)
            {
                throw new ArgumentException("The destination fix is not part of the route", "destinationFix");
            }

            int distance = destinationFixIndex - currentFixIndex;
            if (distance == 0)
            {
                throw new InvalidOperationException("Current fix and destination fix are the same");
            }

            if (Math.Abs(distance) == 1)
            {
                return destinationFix;
            }
            else
            {
                return Fixes[(distance > 0) ? (currentFixIndex + 1) : (currentFixIndex - 1)];
            }
        }

        public int GetDistance(Fix from, Fix to)
        {
            int fromIndex = Fixes.IndexOf(from);
            if (fromIndex < 0)
            {
                throw new ArgumentException("The 'from' fix is not part of the route", "from");
            }

            int toIndex = Fixes.IndexOf(to);
            if (toIndex < 0)
            {
                throw new ArgumentException("The 'to' fix is not part of the route", "to");
            }

            return Math.Abs(toIndex - fromIndex);
        }

        public override bool Equals(object obj)
        {
            Route other = obj as Route;
            if (other == null)
            {
                return false;
            }

            return this.Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
