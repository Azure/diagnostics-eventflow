var AirTrafficControl;
(function (AirTrafficControl) {
    var Airport = (function () {
        function Airport(Name, DisplayName) {
            this.Name = Name;
            this.DisplayName = DisplayName;
        }
        return Airport;
    }());
    AirTrafficControl.Airport = Airport;
})(AirTrafficControl || (AirTrafficControl = {}));
