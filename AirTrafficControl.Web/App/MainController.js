/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="AirplaneState.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    var MainController = (function () {
        function MainController($scope, $interval, $http) {
            var _this = this;
            this.$scope = $scope;
            this.$interval = $interval;
            this.$http = $http;
            $scope.AirplaneStates = [];
            this.updateInterval = $interval(function () { return _this.onUpdateAirplaneStates(); }, 2000);
            $scope.$on('$destroy', function () { return $interval.cancel(_this.updateInterval); });
        }
        MainController.prototype.onUpdateAirplaneStates = function () {
            var _this = this;
            this.$http.get("/api/airplanes").then(function (response) {
                _this.$scope.AirplaneStates = response.data;
            });
            // TODO: some error indication if the data is stale and cannot be refreshed
        };
        return MainController;
    })();
    AirTrafficControl.MainController = MainController;
})(AirTrafficControl || (AirTrafficControl = {}));
