/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="../Scripts/Typings/signalr/signalr.d.ts" />
/// <reference path="../Scripts/Typings/angular-signalr-hub/angular-signalr-hub.d.ts" />
/// <reference path="FlightStatusModel.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    var NewFlightData = (function () {
        function NewFlightData() {
        }
        return NewFlightData;
    }());
    var TrafficSimulationData = (function () {
        function TrafficSimulationData() {
        }
        return TrafficSimulationData;
    }());
    var MainController = (function () {
        function MainController($scope, $interval, $http, Hub) {
            var _this = this;
            this.$scope = $scope;
            this.$interval = $interval;
            this.$http = $http;
            this.Hub = Hub;
            $scope.AirplaneStates = [];
            $scope.$on('$destroy', function () { return _this.onScopeDestroy(); });
            $scope.Airports = [];
            this.$http.get("/api/airports").then(function (response) {
                _this.$scope.Airports = response.data;
            });
            $scope.NewFlightData = new NewFlightData();
            $scope.TrafficSimulationData = new TrafficSimulationData();
            $scope.OnNewFlight = function () { return _this.onNewFlight(); };
            $scope.UpdateSimulation = function () { return _this.updateSimulation(); };
            var atcHubOptions = {
                listeners: {
                    'flightStatusUpdate': function (flightStatus) { return _this.onFlightStatusUpdate(flightStatus); }
                }
            };
            this.atcHub = new Hub('atc', atcHubOptions);
        }
        MainController.prototype.onNewFlight = function () {
            // TODO: validate form parameters before poking the server
            this.$http.post("/api/flights", this.$scope.NewFlightData);
            // TODO: some error indication if the new flight was not created successfully
        };
        MainController.prototype.updateSimulation = function () {
            this.$http.post("/api/simulation/traffic", this.$scope.TrafficSimulationData);
        };
        MainController.prototype.onAnimationProgress = function () {
            if (this.$scope.AnimationProgress < 0.99) {
                this.$scope.AnimationProgress += 1.0 / MainController.AirplaneAnimationPeriods;
            }
        };
        MainController.prototype.onFlightStatusUpdate = function (flightStatus) {
            var _this = this;
            this.$interval.cancel(this.updateInterval);
            this.$scope.AnimationProgress = 0.0;
            var animationDelay = flightStatus.EstimatedNextStatusUpdateDelayMsec / MainController.AirplaneAnimationPeriods;
            this.updateInterval = this.$interval(function () { return _this.onAnimationProgress(); }, animationDelay);
            this.$scope.AirplaneStates = flightStatus.AirplaneStates;
            this.$scope.$apply();
        };
        MainController.prototype.onScopeDestroy = function () {
            this.$interval.cancel(this.updateInterval);
            if (this.$scope.Map) {
                this.$scope.Map.dispose();
            }
        };
        // Number of animation periods per airplane status update period
        MainController.AirplaneAnimationPeriods = 10;
        return MainController;
    }());
    AirTrafficControl.MainController = MainController;
})(AirTrafficControl || (AirTrafficControl = {}));
