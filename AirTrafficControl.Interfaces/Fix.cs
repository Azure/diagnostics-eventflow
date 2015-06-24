using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Interfaces
{
    [DataContract]
    [KnownType(typeof(Airport))]
    public class Fix
    {
        public Fix(string name, string displayName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException();
            }

            Name = name;
            DisplayName = displayName;
        }

        public Fix(string name, string displayName, Location location): this(name, displayName)
        {
            this.Location = location;
        }

        [DataMember]
        public string Name { get; private set; }

        [DataMember]
        public string DisplayName { get; private set; }

        [DataMember]
        public Location Location { get; private set; }

        public override bool Equals(object obj)
        {
            Fix other = obj as Fix;
            if (other == null)
            {
                return false;
            }

            return this.Name == other.Name;
        }

        public static bool operator ==(Fix f1, Fix f2)
        {
            if (object.ReferenceEquals(f1, null))
            {
                return object.ReferenceEquals(f2, null);
            }

            return f1.Equals(f2);
        }

        public static bool operator !=(Fix f1, Fix f2)
        {
            if (object.ReferenceEquals(f1, null))
            {
                return !object.ReferenceEquals(f2, null);
            }

            return !f1.Equals(f2);
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
