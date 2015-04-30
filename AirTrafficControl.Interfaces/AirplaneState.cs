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
        public abstract AirplaneState ComputeNextState(FlightPlan flightPlan, AtcInstruction instruction);
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
    }

    [DataContract]
    public class HoldingState : FixLocationState
    {
        public HoldingState(Fix fix) : base(fix) { }

        public override AirplaneState ComputeNextState(FlightPlan flightPlan, AtcInstruction instruction)
        {
            ValidateComputeNextStateArgs(flightPlan, instruction);

            Route route = Universe.Current.GetRouteBetween(this.Fix, flightPlan.Destination);
            if (route == null)
            {
                throw new ArgumentException("Cannot find route between " + this.Fix.DisplayName + " and " + flightPlan.Destination.DisplayName);
            }
            Fix next = route.GetNextFix(this.Fix, flightPlan.Destination);

            EnrouteClearance enrouteClearance = instruction as EnrouteClearance;
            if (enrouteClearance != null)
            {
                if (enrouteClearance.IsClearedTo(this.Fix, next, route))
                {
                    return new EnrouteState(this.Fix, next, route);
                }
            }

            ApproachClearance approachClearance = null;
            if (approachClearance != null)
            {
                if (next == approachClearance.LocationOrLimit)
                {
                    return new ApproachState(approachClearance.LocationOrLimit);
                }
            }

            // By default continue holding
            return this;
        }

        public override string ToString()
        {
            return "Holding at " + Fix.DisplayName;
        }
    }

    [DataContract]
    public class ApproachState: AirportLocationState
    {
        public ApproachState(Airport airport) : base(airport) { }

        public override AirplaneState ComputeNextState(FlightPlan flightPlan, Interfaces.AtcInstruction instruction)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "Flying approach to " + Airport.DisplayName;
        }
    }

    [DataContract]
    public class LandedState: AirportLocationState
    {
        public LandedState(Airport airport) : base(airport) { }

        public override AirplaneState ComputeNextState(FlightPlan flightPlan, Interfaces.AtcInstruction instruction)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "Landed at " + Airport.DisplayName;
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

        public override string ToString()
        {
            return "Flying on route " + Route.Name + " from " + From.DisplayName + " to " + To.DisplayName;
        }

        public override AirplaneState ComputeNextState(FlightPlan flightPlan, AtcInstruction instruction)
        {
            throw new NotImplementedException();
        }
    }
}
