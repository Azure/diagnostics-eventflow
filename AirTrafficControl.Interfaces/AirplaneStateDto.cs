using Validation;

namespace AirTrafficControl.Interfaces
{
    public class AirplaneStateDto
    {
        // Parameterless constructor for deserialization
        public AirplaneStateDto()
        {

        }

        public AirplaneStateDto(string id, string stateDescription, Location position, double heading)
        {
            Requires.NotNullOrWhiteSpace(id, nameof(id));

            this.ID = id;
            this.StateDescription = stateDescription;
            this.Location = position;
            this.Heading = heading;
        }

        public AirplaneStateDto(AirplaneState airplaneState, FlightPlan flightPlan)
        {
            Requires.NotNull(airplaneState, nameof(airplaneState));
            Requires.NotNull(flightPlan, nameof(flightPlan));

            this.ID = flightPlan.AirplaneID;
            this.StateDescription = airplaneState.ToString();
            this.Location = airplaneState.Location;
            this.Heading = airplaneState.GetHeading(flightPlan);
        }

        public string ID { get; set; }
        public string StateDescription { get; set; }

        public Location Location { get; set; }

        // Heading (in radians), 360 is zero and increases clockwise
        public double Heading { get; set; }
    }
}
