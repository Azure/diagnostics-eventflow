/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="MainController.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    (function () {
        var app = angular.module('AtcAppDirectives', []);
        app.directive('bingMap', function () {
            return {
                restrict: 'AC',
                link: function (scope, instanceElement, instanceAttributes, controller, transclude) {
                    scope.Map = new Microsoft.Maps.Map(instanceElement[0], {
                        credentials: instanceAttributes["bing-maps-key"],
                        zoom: 8,
                        disableZooming: true,
                        center: new Microsoft.Maps.Location(47.610, -122.232)
                    });
                }
            };
        });
    })();
})(AirTrafficControl || (AirTrafficControl = {}));
