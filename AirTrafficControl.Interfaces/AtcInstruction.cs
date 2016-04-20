using Microsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Validation;

namespace AirTrafficControl.Interfaces
{
    [DataContract]
    [KnownType(typeof(AirportFixAtcInstruction))]
    [KnownType(typeof(TakeoffClearance))]
    [KnownType(typeof(HoldInstruction))]
    [KnownType(typeof(EnrouteClearance))]
    [KnownType(typeof(ApproachClearance))]
    public abstract class AtcInstruction
    {
        public AtcInstruction(Fix locationOrLimit)
        {
            Requires.NotNull(locationOrLimit, "locationOrLimit");
            this.LocationOrLimit = locationOrLimit;
        }

        [DataMember]
        public Fix LocationOrLimit { get; private set; }
    }

    [DataContract]
    public abstract class AirportFixAtcInstruction: AtcInstruction
    {
        public AirportFixAtcInstruction(Airport airport) : base(airport) { this.LocationOrLimit = airport; }

        [DataMember]
        public new Airport LocationOrLimit { get; private set; }
    }

    [DataContract]
    public class TakeoffClearance: AirportFixAtcInstruction
    {
        public TakeoffClearance(Airport airport) : base(airport) { }

        public override string ToString()
        {
            return "Cleared for takeoff at " + LocationOrLimit.DisplayName;
        }
    }

    [DataContract]
    public class HoldInstruction: AtcInstruction
    {
        public HoldInstruction(Fix fix) : base(fix) { }

        public override string ToString()
        {
            return "Hold at " + LocationOrLimit.DisplayName;
        }
    }

    [DataContract]
    public class EnrouteClearance: AtcInstruction
    {
        private IList<Fix> flightPath;

        public EnrouteClearance(Fix limit, IList<Fix> flightPath) : base(limit)
        {
            Requires.NotNull(flightPath, nameof(flightPath));
            this.flightPath = flightPath;
            Requires.ValidState(flightPath.Contains(limit), "The flight path must contain the clearance limit");
        }

        public override string ToString()
        {
            return "Cleared to " + LocationOrLimit.DisplayName;
        }

        public bool IsClearedTo(Fix current, Fix target)
        {
            Requires.NotNull(current, nameof(current));
            Requires.NotNull(target, nameof(target));
            Requires.That(this.flightPath.Contains(current), nameof(current), "The 'current' fix is not part of the flight path");
            Requires.That(this.flightPath.Contains(target), nameof(target), "The 'target' fix is not part of the flight path");
            Assumes.False(current == target, "Current fix cannot be the same as the target fix");

            int limitIndex = this.flightPath.IndexOf(this.LocationOrLimit);
            int currentIndex = this.flightPath.IndexOf(current);
            int targetIndex = this.flightPath.IndexOf(target);

            bool sameDirection = currentIndex < targetIndex;
            bool notGoingBeyondTheLimit = targetIndex <= limitIndex;
            return sameDirection && notGoingBeyondTheLimit;
        }
    }

    [DataContract]
    public class ApproachClearance: AirportFixAtcInstruction
    {
        public ApproachClearance(Airport airport) : base(airport) { }

        public override string ToString()
        {
            return "Cleared for the approach to " + LocationOrLimit.DisplayName;
        }
    }
}
