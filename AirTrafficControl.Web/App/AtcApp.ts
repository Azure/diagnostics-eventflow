
/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 

module AirTrafficControl {

    (function () {
        var app = angular.module('AtcApp', []);

        app.controller("MainController", ["$scope", "$interval", "$http", MainController]);
    })();
}

