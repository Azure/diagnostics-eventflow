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
        // CONSIDER Should enroute clearance also have a Route?
        public EnrouteClearance(Fix limit) : base(limit) { }

        public override string ToString()
        {
            return "Cleared to " + LocationOrLimit.DisplayName;
        }

        public bool IsClearedTo(Fix current, Fix target, Route route)
        {
            Requires.NotNull(current, "current");
            Requires.NotNull(target, "to");
            Requires.NotNull(route, "route");
            Assumes.True(route.Fixes.Contains(current), "The 'current' fix is not part of the route");
            Assumes.True(route.Fixes.Contains(target), "The 'target' fix is not part of the route");
            Assumes.False(current == target, "Current fix cannot be the same as the target fix");

            if (!route.Fixes.Contains(this.LocationOrLimit))
            {
                return false;
            }

            int distanceToTarget = route.GetDirectionalDistance(current, target);
            int distanceToLimit = route.GetDirectionalDistance(current, this.LocationOrLimit);

            bool sameDirection = (distanceToLimit * distanceToTarget) > 0;
            bool notGoingBeyondTheLimit = Math.Abs(distanceToTarget) <= Math.Abs(distanceToLimit);
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
