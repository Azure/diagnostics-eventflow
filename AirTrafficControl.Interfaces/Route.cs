using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Microsoft;
using Validation;

namespace AirTrafficControl.Interfaces
{
    [DataContract]
    public class Route
    {
        private ReadOnlyCollection<Fix> fixes;

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
        public ReadOnlyCollection<Fix> Fixes
        {
            get { return this.fixes; }
            set
            {
                if (value == null || value.Count < 2)
                {
                    throw new ArgumentException("A route must have at least 2 fixes");
                }
                this.fixes = value;
            }
        }

        public IEnumerable<Fix> GetAdjacentFixes(Fix reference)
        {
            Requires.NotNull(reference, nameof(reference));

            int referenceIndex = Fixes.IndexOf(reference);
            Assumes.True(referenceIndex >= 0, "The 'reference' fix is not part of the route");

            if (referenceIndex == 0)
            {
                return new[] { Fixes[1] };
            }
            else if (referenceIndex == Fixes.Count - 1)
            {
                return new[] { Fixes[Fixes.Count - 2] };
            }
            else
            {
                return new[] { Fixes[referenceIndex - 1], Fixes[referenceIndex + 1] };
            }
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
