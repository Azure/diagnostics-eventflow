/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="AirplaneState.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    var MainController = (function () {
        function MainController($scope) {
            this.$scope = $scope;
            this.AirplaneStates = [];
            this.AirplaneStates.push(new AirTrafficControl.AirplaneState("489Y", "Flying from Seattle to Portland"));
            this.AirplaneStates.push(new AirTrafficControl.AirplaneState("705JA", "Flying from Spokane to Moses Lake"));
        }
        return MainController;
    })();
    AirTrafficControl.MainController = MainController;
})(AirTrafficControl || (AirTrafficControl = {}));
