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
    [KnownType(typeof(Taxiing))]
    [KnownType(typeof(Departing))]
    [KnownType(typeof(Holding))]
    [KnownType(typeof(Approach))]
    [KnownType(typeof(Landed))]
    [KnownType(typeof(Enroute))]
    public abstract class AirplaneState
    {
        public abstract AirplaneState ComputeNextState(FlightPlan flightPlan, AtcInstruction instruction);
        protected void ValidateComputeNextStateArgs(FlightPlan flightPlan, AtcInstruction instruction)
        {
            if (flightPlan == null)
            {
                throw new ArgumentNullException("flightPlan");
            }

            if (flightPlan.DeparturePoint == flightPlan.Destination)
            {
                throw new ArgumentException("The flight plan is invalid, departure point and destination cannot be the same");
            }

            // It is OK for the instruction to be null, it just means we should proceed accorting to the plan and current state
        }
    }

    [DataContract]
    public abstract class AirportLocationState : AirplaneState
    {
        public AirportLocationState(Airport airport)
        {
            if (airport == null)
            {
                throw new ArgumentNullException("airport");
            }

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
            if (fix == null)
            {
                throw new ArgumentNullException("fix");
            }

            this.Fix = fix;
        }


        [DataMember]
        public Fix Fix { get; private set; }
    }

    [DataContract]
    public class Taxiing : AirportLocationState
    {
        public Taxiing(Airport airport) : base(airport) { }

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
                var newState = new Departing(this.Airport);
                return newState;
            }
        }

        public override string ToString()
        {
            return "Taxiing at " + Airport.DisplayName;
        }
    }

    [DataContract]
    public class Departing : AirportLocationState
    {
        public Departing(Airport airport) : base(airport) { }

        public override AirplaneState ComputeNextState(FlightPlan flightPlan, AtcInstruction instruction)
        {
            ValidateComputeNextStateArgs(flightPlan, instruction);

            // Hold at the airport if we have received a holding instruction
            HoldInstruction holdInstruction = instruction as HoldInstruction;
            if (holdInstruction != null && holdInstruction.LocationOrLimit == this.Airport)
            {
                return new Holding(this.Airport);
            }
            else
            {
                Route route = Universe.Current.GetRouteBetween(flightPlan.DeparturePoint, flightPlan.Destination);
                if (route == null)
                {
                    throw new ArgumentException("The flight plan is invalid, there is no route between " + flightPlan.DeparturePoint.DisplayName + " and " + flightPlan.Destination.DisplayName);
                }

                Fix next = route.GetNextFix(this.Airport, flightPlan.Destination);
                return new 
            }
        }

        public override string ToString()
        {
            return "Departing from " + Airport.DisplayName;
        }
    }

    [DataContract]
    public class Holding : FixLocationState
    {
        public Holding(Fix fix) : base(fix) { }

        public override string ToString()
        {
            return "Holding at " + Fix.DisplayName;
        }
    }

    [DataContract]
    public class Approach: AirportLocationState
    {
        public override string ToString()
        {
            return "Flying approach to " + Airport.DisplayName;
        }
    }

    [DataContract]
    public class Landed: AirportLocationState
    {
        public override string ToString()
        {
            return "Landed at " + Airport.DisplayName;
        }
    }

    [DataContract]
    public class Enroute: AirplaneState
    {
        public Enroute(Fix from, Fix to, Route route)
        {
            if (route == null)
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
