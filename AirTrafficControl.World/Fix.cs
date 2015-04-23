using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.World
{
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

        public string Name { get; private set; }
        public string DisplayName { get; private set; }

        public override bool Equals(object obj)
        {
            Fix other = obj as Fix;
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
