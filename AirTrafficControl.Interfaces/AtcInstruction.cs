using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Interfaces
{
    [DataContract]
    [KnownType(typeof(TakeoffClearance))]
    [KnownType(typeof(HoldInstruction))]
    [KnownType(typeof(EnrouteClearance))]
    [KnownType(typeof(ApproachClearance))]
    public abstract class AtcInstruction
    {
        [DataMember]
        public Fix LocationOrLimit { get; }
    }

    [DataContract]
    public class TakeoffClearance: AtcInstruction
    {
        public override string ToString()
        {
            return "Cleared for takeoff at " + LocationOrLimit.DisplayName;
        }
    }

    [DataContract]
    public class HoldInstruction: AtcInstruction
    {
        public override string ToString()
        {
            return "Hold at " + LocationOrLimit.DisplayName;
        }
    }

    [DataContract]
    public class EnrouteClearance: AtcInstruction
    {
        public override string ToString()
        {
            return "Cleared to " + LocationOrLimit.DisplayName;
        }
    }

    [DataContract]
    public class ApproachClearance: AtcInstruction
    {
        public override string ToString()
        {
            return "Cleared for the approach to " + LocationOrLimit.DisplayName;
        }
    }
}
