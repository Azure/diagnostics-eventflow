var AirTrafficControl;
(function (AirTrafficControl) {
    var Position = (function () {
        function Position(Latitude, Longitude, Altitude) {
            this.Latitude = Latitude;
            this.Longitude = Longitude;
            this.Altitude = Altitude;
        }
        return Position;
    })();
    AirTrafficControl.Position = Position;
    var AirplaneState = (function () {
        function AirplaneState(ID, StateDescription, Location) {
            this.ID = ID;
            this.StateDescription = StateDescription;
            this.Location = Location;
        }
        return AirplaneState;
    })();
    AirTrafficControl.AirplaneState = AirplaneState;
})(AirTrafficControl || (AirTrafficControl = {}));
