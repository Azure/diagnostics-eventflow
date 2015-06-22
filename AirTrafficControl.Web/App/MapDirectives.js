/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="MainController.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    (function () {
        var app = angular.module('AtcAppDirectives', []);
        app.directive('bingMap', function () {
            return {
                restrict: 'A',
                link: function (scope, instanceElement, instanceAttributes, controller, transclude) {
                    scope.GetBingMapsKey().then(function (key) {
                        scope.Map = new Microsoft.Maps.Map(instanceElement[0], {
                            credentials: key
                        });
                    });
                }
            };
        });
    })();
})(AirTrafficControl || (AirTrafficControl = {}));
