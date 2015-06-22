/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="AirplaneState.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    var NewFlightData = (function () {
        function NewFlightData() {
        }
        return NewFlightData;
    })();
    var MainController = (function () {
        function MainController($scope, $interval, $http) {
            var _this = this;
            this.$scope = $scope;
            this.$interval = $interval;
            this.$http = $http;
            $scope.AirplaneStates = [];
            this.updateInterval = $interval(function () { return _this.onUpdateAirplaneStates(); }, 2000);
            $scope.$on('$destroy', function () { return $interval.cancel(_this.updateInterval); });
            $scope.Airports = [];
            this.$http.get("/api/airports").then(function (response) {
                _this.$scope.Airports = response.data;
            });
            $scope.NewFlightData = new NewFlightData();
            $scope.OnNewFlight = function () { return _this.onNewFlight(); };
            $scope.GetBingMapsKey = function () { return _this.getBingMapsKey(); };
        }
        MainController.prototype.onUpdateAirplaneStates = function () {
            var _this = this;
            this.$http.get("/api/airplanes").then(function (response) {
                _this.$scope.AirplaneStates = response.data;
            });
            // TODO: some error indication if the data is stale and cannot be refreshed
        };
        MainController.prototype.onNewFlight = function () {
            // TODO: validate form parameters before poking the server
            this.$http.post("/api/flights", this.$scope.NewFlightData);
            // TODO: some error indication if the new flight was not created successfully
        };
        MainController.prototype.getBingMapsKey = function () {
            return this.$http.get("/api/bingmapskey").then(function (response) {
                return response.data;
            });
        };
        return MainController;
    })();
    AirTrafficControl.MainController = MainController;
})(AirTrafficControl || (AirTrafficControl = {}));
