using Validation;

namespace AirTrafficControl.Interfaces
{
    public class AirplaneStateDto
    {
        public AirplaneStateDto(string id, string stateDescription, Location position, double heading)
        {
            Requires.NotNullOrWhiteSpace(id, "id");

            this.ID = id;
            this.StateDescription = stateDescription;
            this.Location = position;
            this.Heading = heading;
        }

        public string ID { get; private set; }
        public string StateDescription { get; private set; }

        public Location Location { get; private set; }

        // Heading (in radians), 360 is zero and increases clockwise
        public double Heading { get; private set; }
    }
}
