
/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="MainController.ts" />

module AirTrafficControl {
    (function () {
        var app = angular.module('AtcAppDirectives', []);

        app.directive('bingMap',(): ng.IDirective => {

            return {
                restrict: 'AC',

                link: (
                    scope: IMainControllerScope,
                    instanceElement: ng.IAugmentedJQuery,
                    instanceAttributes: ng.IAttributes,
                    controller: any,
                    transclude: ng.ITranscludeFunction
                    ) => {

                    var onAirplaneStateChanged = function () {
                    };
                    
                    scope.Map = new Microsoft.Maps.Map(instanceElement[0], {
                        credentials: instanceAttributes["bing-maps-key"],
                        zoom: 8,
                        disableZooming: true,
                        center: new Microsoft.Maps.Location(47.610, -122.232)
                    });
                    
                    scope.$watch("AirplaneStates",(newAirplaneStates, oldAirplaneStates, scope: IMainControllerScope) => {
                        // TODO update airplane positions based on what is in scope.AirplaneStates
                        var map = scope.Map;
                        map.entities.clear();
                        var options = { strokeColor: new Microsoft.Maps.Color(0, 0, 255, Math.round(255 * Math.random())), strokeThickness: parseInt(thickness) };
                        var polyline = new Microsoft.Maps.Polyline([new Microsoft.Maps.Location(latlon.latitude - 0.1, latlon.longitude - 0.1), new Microsoft.Maps.Location(latlon.latitude + 0.1, latlon.longitude - 0.1), new Microsoft.Maps.Location(latlon.latitude + 0.1, latlon.longitude), new Microsoft.Maps.Location(latlon.latitude - 0.1, latlon.longitude), new Microsoft.Maps.Location(latlon.latitude - 0.1, latlon.longitude + 0.1), new Microsoft.Maps.Location(latlon.latitude + 0.1, latlon.longitude + 0.1)], options);
                        map.setView({ zoom: 10 });
                        map.entities.push(polyline);
                    });
                }
            };
        });
    })();
}