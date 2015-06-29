
/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="MainController.ts" />

module AirTrafficControl {
    (function () {
        var app = angular.module('AtcAppDirectives', []);
        var seattleLocation = new Microsoft.Maps.Location(47.610, -122.232)

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
                        credentials: instanceAttributes["bingMapsKey"],
                        zoom: 10,
                        disableZooming: true,
                        center: seattleLocation
                    });

                    var options = { strokeColor: new Microsoft.Maps.Color(255, 0, 0, 255), strokeThickness: 3 };
                    var polyline = new Microsoft.Maps.Polyline([
                        new Microsoft.Maps.Location(seattleLocation.latitude - 0.1, seattleLocation.longitude - 0.1),
                        new Microsoft.Maps.Location(seattleLocation.latitude + 0.1, seattleLocation.longitude - 0.1),
                        new Microsoft.Maps.Location(seattleLocation.latitude + 0.1, seattleLocation.longitude),
                        new Microsoft.Maps.Location(seattleLocation.latitude - 0.1, seattleLocation.longitude),
                        new Microsoft.Maps.Location(seattleLocation.latitude - 0.1, seattleLocation.longitude + 0.1),
                        new Microsoft.Maps.Location(seattleLocation.latitude + 0.1, seattleLocation.longitude + 0.1)],
                        options);
                    scope.Map.entities.push(polyline);
                    
                    scope.$watch("AirplaneStates",(newAirplaneStates, oldAirplaneStates, scope: IMainControllerScope) => {
                        
                        var map = scope.Map;
                        // map.entities.clear();

                        // TODO update airplane positions based on what is in scope.AirplaneStates
                    });
                }
            };
        });
    })();
}