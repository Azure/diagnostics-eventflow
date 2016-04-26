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
    var Fix = (function () {
        function Fix(Location) {
            this.Location = Location;
        }
        return Fix;
    }());
    AirTrafficControl.Fix = Fix;
    var AirplaneState = (function () {
        function AirplaneState(ID, StateDescription, Location, Heading, EnrouteFrom, EnrouteTo) {
            this.ID = ID;
            this.StateDescription = StateDescription;
            this.Location = Location;
            this.Heading = Heading;
            this.EnrouteFrom = EnrouteFrom;
            this.EnrouteTo = EnrouteTo;
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
