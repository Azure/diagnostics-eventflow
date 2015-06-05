var AirTrafficControl;
(function (AirTrafficControl) {
    var AirplaneState = (function () {
        function AirplaneState(ID, StateDescription) {
            this.ID = ID;
            this.StateDescription = StateDescription;
        }
        return AirplaneState;
    })();
    AirTrafficControl.AirplaneState = AirplaneState;
})(AirTrafficControl || (AirTrafficControl = {}));
