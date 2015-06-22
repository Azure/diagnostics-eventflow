
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
                    scope.GetBingMapsKey().then((key) => {
                        scope.Map = new Microsoft.Maps.Map(instanceElement[0], {
                            credentials: key
                        });
                    });                    
                }
            };
        });
    })();
}