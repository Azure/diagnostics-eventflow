/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="../Scripts/Typings/signalr/signalr.d.ts" />
/// <reference path="../Scripts/Typings/angular-signalr-hub/angular-signalr-hub.d.ts" />
/// <reference path="AirplaneState.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    var NewFlightData = (function () {
        function NewFlightData() {
        }
        return NewFlightData;
    })();
    var MainController = (function () {
        function MainController($scope, $interval, $http, Hub) {
            var _this = this;
            this.$scope = $scope;
            this.$interval = $interval;
            this.$http = $http;
            this.Hub = Hub;
            $scope.AirplaneStates = [];
            // Temporarily disable polling while experimenting with SignalR
            // this.updateInterval = $interval(() => this.onUpdateAirplaneStates(), 2000);
            $scope.$on('$destroy', function () { return _this.onScopeDestroy(); });
            $scope.Airports = [];
            this.$http.get("/api/airports").then(function (response) {
                _this.$scope.Airports = response.data;
            });
            $scope.NewFlightData = new NewFlightData();
            $scope.OnNewFlight = function () { return _this.onNewFlight(); };
            var atcHubOptions = {
                listeners: {
                    'airplaneStateUpdate': function (newAirplaneStates) {
                        _this.$scope.AirplaneStates = newAirplaneStates;
                        _this.$scope.$apply();
                    }
                }
            };
            this.atcHub = new Hub('atc', atcHubOptions);
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
        MainController.prototype.onScopeDestroy = function () {
            this.$interval.cancel(this.updateInterval);
            if (this.$scope.Map) {
                this.$scope.Map.dispose();
            }
        };
        return MainController;
    })();
    AirTrafficControl.MainController = MainController;
})(AirTrafficControl || (AirTrafficControl = {}));
