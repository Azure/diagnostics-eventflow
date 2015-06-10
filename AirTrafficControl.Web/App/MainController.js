/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="AirplaneState.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    var MainController = (function () {
        function MainController($scope, $interval) {
            var _this = this;
            this.$scope = $scope;
            this.$interval = $interval;
            $scope.AirplaneStates = [];
            this.updateInterval = $interval(function () { return _this.onUpdateAirplaneStates(); }, 2000);
            $scope.$on('$destroy', function () { return $interval.cancel(_this.updateInterval); });
        }
        MainController.prototype.onUpdateAirplaneStates = function () {
            this.$scope.AirplaneStates = [new AirTrafficControl.AirplaneState("489Y", "Flying. Time is " + new Date().toISOString())];
        };
        return MainController;
    })();
    AirTrafficControl.MainController = MainController;
})(AirTrafficControl || (AirTrafficControl = {}));
