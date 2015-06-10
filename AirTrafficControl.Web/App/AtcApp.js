/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
var AirTrafficControl;
(function (AirTrafficControl) {
    (function () {
        var app = angular.module('AtcApp', []);
        app.controller("MainController", ["$scope", "$interval", AirTrafficControl.MainController]);
    })();
})(AirTrafficControl || (AirTrafficControl = {}));
