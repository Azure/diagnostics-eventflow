
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
                        // TODO update airplane positions
                    });
                }
            };
        });
    })();
}