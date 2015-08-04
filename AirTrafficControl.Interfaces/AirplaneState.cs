using Microsoft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Interfaces
{
    [DataContract]
    [KnownType(typeof(TaxiingState))]
    [KnownType(typeof(DepartingState))]
    [KnownType(typeof(HoldingState))]
    [KnownType(typeof(ApproachState))]
    [KnownType(typeof(LandedState))]
    [KnownType(typeof(EnrouteState))]
    public abstract class AirplaneState
    {
        public abstract Location Location { get; }        

        public abstract AirplaneState ComputeNextState(FlightPlan flightPlan, AtcInstruction instruction);
        public abstract double GetHeading(FlightPlan flightPlan);

        protected void ValidateComputeNextStateArgs(FlightPlan flightPlan, AtcInstruction instruction)
        {
            Requires.NotNull(flightPlan, "flightPlan");
            Requires.Argument(flightPlan.DeparturePoint != flightPlan.Destination, "flightPlan", "The flight plan is invalid, departure point and destination cannot be the same");

            // It is OK for the instruction to be null, it just means we should proceed accorting to the plan and current state
        }
    }

    [DataContract]
    public abstract class AirportLocationState : AirplaneState
    {
        public AirportLocationState(Airport airport)
        {
            Requires.NotNull(airport, "airport");
            this.Airport = airport;
        }

        [DataMember]
        public Airport Airport { get; private set; }

        public override Location Location { get { return Airport.Location; } }
    }

    [DataContract]
    public abstract class FixLocationState : AirplaneState
    {
        public FixLocationState(Fix fix)
        {
            Requires.NotNull(fix, "fix");
            this.Fix = fix;
        }

        [DataMember]
        public Fix Fix { get; private set; }

        public override Location Location { get { return Fix.Location; } }
    }

    [DataContract]
    public class TaxiingState : AirportLocationState
    {
        public TaxiingState(Airport airport) : base(airport) { }

        public override AirplaneState ComputeNextState(FlightPlan flightPlan, AtcInstruction instruction)
        {
            ValidateComputeNextStateArgs(flightPlan, instruction);

            TakeoffClearance clearance = instruction as TakeoffClearance;            
            if (clearance == null || clearance.LocationOrLimit != this.Airport)
            {
                Debug.Assert(clearance == null && instruction == null, "We have received an unexpected instruction or a takeoff clearance for the wrong airport");
                return this;  // Waiting for takeoff clearance
            }
            else
            {
                var newState = new DepartingState(this.Airport);
                return newState;
            }
        }

        public override string ToString()
        {
            return "Taxiing at " + Airport.DisplayName;
        }

        public override double GetHeading(FlightPlan flightPlan)
        {
            return 0.0; // We do not track heading changes while taxiing.
        }
    }

    [DataContract]
    public class DepartingState : AirportLocationState
    {
        public DepartingState(Airport airport) : base(airport) { }

        public override AirplaneState ComputeNextState(FlightPlan flightPlan, AtcInstruction instruction)
        {
            ValidateComputeNextStateArgs(flightPlan, instruction);

            // Hold at the airport if we have received a holding instruction
            HoldInstruction holdInstruction = instruction as HoldInstruction;
            if (holdInstruction != null && holdInstruction.LocationOrLimit == this.Airport)
            {
                return new HoldingState(this.Airport);
            }
            else
            {
                Route route = Universe.Current.GetRouteBetween(flightPlan.DeparturePoint, flightPlan.Destination);
                if (route == null)
                {
                    throw new ArgumentException("The flight plan is invalid, there is no route between " + flightPlan.DeparturePoint.DisplayName + " and " + flightPlan.Destination.DisplayName);
                }

                Fix next = route.GetNextFix(this.Airport, flightPlan.Destination);
                return new EnrouteState(this.Airport, next, route);
            }
        }

        public override string ToString()
        {
            return "Departing from " + Airport.DisplayName;
        }

        public override double GetHeading(FlightPlan flightPlan)
        {
            return this.Airport.Location.GetDirectHeadingTo(flightPlan.Destination.Location);
        }
    }

    [DataContract]
    public class HoldingState : FixLocationState
    {
        public HoldingState(Fix fix) : base(fix) { }

        public override AirplaneState ComputeNextState(FlightPlan flightPlan, AtcInstruction instruction)
        {
            ValidateComputeNextStateArgs(flightPlan, instruction);

            ApproachClearance approachClearance = instruction as ApproachClearance;
            if (approachClearance != null)
            {
                if (this.Fix == approachClearance.LocationOrLimit)
                {
                    return new ApproachState(approachClearance.LocationOrLimit);
                }
            }

            EnrouteClearance enrouteClearance = instruction as EnrouteClearance;
            if (enrouteClearance != null)
            {
                Route route = Universe.Current.GetRouteBetween(this.Fix, flightPlan.Destination);
                Assumes.NotNull(route);
                Fix next = route.GetNextFix(this.Fix, flightPlan.Destination);

                if (enrouteClearance.IsClearedTo(this.Fix, next, route))
                {
                    return new EnrouteState(this.Fix, next, route);
                }
            }

            // By default continue holding
            return this;
        }

        public override string ToString()
        {
            return "Holding at " + Fix.DisplayName;
        }

        public override double GetHeading(FlightPlan flightPlan)
        {
            return flightPlan.DeparturePoint.Location.GetDirectHeadingTo(flightPlan.Destination.Location);
        }
    }

    [DataContract]
    public class ApproachState: AirportLocationState
    {
        public ApproachState(Airport airport) : base(airport) { }

        public override AirplaneState ComputeNextState(FlightPlan flightPlan, Interfaces.AtcInstruction instruction)
        {
            // Approaches are always successful, we never go missed :-)
            return new LandedState(this.Airport);
        }

        public override string ToString()
        {
            return "Flying approach to " + Airport.DisplayName;
        }

        public override double GetHeading(FlightPlan flightPlan)
        {
            return flightPlan.DeparturePoint.Location.GetDirectHeadingTo(flightPlan.Destination.Location);
        }
    }

    [DataContract]
    public class LandedState: AirportLocationState
    {
        public LandedState(Airport airport) : base(airport) { }

        public override AirplaneState ComputeNextState(FlightPlan flightPlan, Interfaces.AtcInstruction instruction)
        {
            // After landing the airplane just disappears from the world
            return null;
        }

        public override string ToString()
        {
            return "Landed at " + Airport.DisplayName;
        }

        public override double GetHeading(FlightPlan flightPlan)
        {
            return 0.0;
        }
    }

    [DataContract]
    public class EnrouteState: AirplaneState
    {
        public EnrouteState(Fix from, Fix to, Route route)
        {
            Requires.NotNull(from, "from");
            Requires.NotNull(to, "to");
            Requires.NotNull(route, "route");
            Requires.Argument(route.Fixes.Contains(from), "from", "The 'from' Fix is not part of the passed Route");
            Requires.Argument(route.Fixes.Contains(to), "to", "The 'to' Fix is not part of the passed Route");

            this.From = from;
            this.To = to;
            this.Route = route;
        }


        [DataMember]
        public Fix From { get; private set; }

        [DataMember]
        public Fix To { get; private set; }

        [DataMember]
        public Route Route { get; private set; }

        public override Location Location
        {
            get
            {
                // Just an aproximation
                return new Location(
                    (this.To.Location.Latitude + this.From.Location.Latitude) / 2.0,
                    (this.To.Location.Longitude + this.From.Location.Longitude) / 2.0
                );
            }
        }

        public override string ToString()
        {
            return "Flying on route " + Route.Name + " from " + From.DisplayName + " to " + To.DisplayName;
        }

        public override AirplaneState ComputeNextState(FlightPlan flightPlan, AtcInstruction instruction)
        {
            ValidateComputeNextStateArgs(flightPlan, instruction);

            if (this.To == flightPlan.Destination)
            {
                ApproachClearance clearance = instruction as ApproachClearance;
                if (clearance != null && clearance.LocationOrLimit == flightPlan.Destination)
                {
                    return new ApproachState(flightPlan.Destination);
                }
                else
                {
                    return new HoldingState(flightPlan.Destination);
                }
            }
            else
            {
                Fix next = Route.GetNextFix(this.To, flightPlan.Destination);
                return new EnrouteState(this.To, next, this.Route);
            }
        }

        public override double GetHeading(FlightPlan flightPlan)
        {
            return From.Location.GetDirectHeadingTo(To.Location);
        }
    }
}
