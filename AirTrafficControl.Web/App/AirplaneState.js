var AirTrafficControl;
(function (AirTrafficControl) {
    var Position = (function () {
        function Position(Latitude, Longitude, Altitude) {
            this.Latitude = Latitude;
            this.Longitude = Longitude;
            this.Altitude = Altitude;
        }
        return Position;
    }());
    AirTrafficControl.Position = Position;
    var AirplaneState = (function () {
        function AirplaneState(ID, StateDescription, Location, Heading) {
            this.ID = ID;
            this.StateDescription = StateDescription;
            this.Location = Location;
            this.Heading = Heading;
        }
        return AirplaneState;
    }());
    AirTrafficControl.AirplaneState = AirplaneState;
    var FlightStatusModel = (function () {
        function FlightStatusModel(AirplaneStates, EstimatedNextStatusUpdateDelayMsec) {
            this.AirplaneStates = AirplaneStates;
            this.EstimatedNextStatusUpdateDelayMsec = EstimatedNextStatusUpdateDelayMsec;
        }
        return FlightStatusModel;
    }());
    AirTrafficControl.FlightStatusModel = FlightStatusModel;
})(AirTrafficControl || (AirTrafficControl = {}));
